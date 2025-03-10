using AutoMapper;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Domain.Repositories;
using MediatR;
using FluentValidation;
using Microsoft.Extensions.Logging;
using BitcoinPriceAggregator.Domain.Services;

namespace BitcoinPriceAggregator.Application.Queries
{
    public class GetPriceRangeQueryHandler : IRequestHandler<GetPriceRangeQuery, IEnumerable<BitcoinPriceDto>>
    {
        private readonly IBitcoinPriceRepository _repository;
        private readonly IPriceAggregatorService _aggregatorService;
        private readonly IMapper _mapper;
        private readonly IValidator<GetPriceRangeQuery> _validator;
        private readonly ILogger<GetPriceRangeQueryHandler> _logger;

        public GetPriceRangeQueryHandler(
            IBitcoinPriceRepository repository,
            IPriceAggregatorService aggregatorService,
            IMapper mapper,
            IValidator<GetPriceRangeQuery> validator,
            ILogger<GetPriceRangeQueryHandler> logger)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _aggregatorService = aggregatorService ?? throw new ArgumentNullException(nameof(aggregatorService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public async Task<IEnumerable<BitcoinPriceDto>> Handle(GetPriceRangeQuery request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            try
            {
                var cachedPrices = await _repository.GetPriceRangeAsync(request.StartTicks, request.EndTicks, request.Pair);
                var cachedPricesByTimestamp = cachedPrices.ToDictionary(p => p.UtcTicks);


                // Get all required timestamps based on what we already have
                var allTimestamps = GetTimePoints(request.StartTicks, request.EndTicks, cachedPricesByTimestamp).ToList();
                var missingTimestamps = allTimestamps.Where(t => !cachedPricesByTimestamp.ContainsKey(t)).ToList();

                // Only fetch missing prices if there are any
                if (missingTimestamps.Count > 0)
                {
                    _logger.LogInformation("Fetching {Count} missing prices for {Pair} between {StartTime} and {EndTime}",
                        missingTimestamps.Count, request.Pair, request.StartTicks, request.EndTicks);

                    foreach (var timestamp in missingTimestamps)
                    {
                        // Fetch individual missing price
                        var price = await _aggregatorService.GetAggregatedPriceAsync(timestamp, request.Pair);
                        await _repository.StorePriceAsync(price, cancellationToken);
                        cachedPricesByTimestamp[price.UtcTicks] = price;
                        _logger.LogDebug("Successfully fetched and stored price for {Timestamp} and {Pair}", timestamp, request.Pair);
                    }
                }

                // Return all prices ordered by timestamp
                var allPrices = cachedPricesByTimestamp.Values.OrderBy(p => p.UtcTicks);
                return _mapper.Map<IEnumerable<BitcoinPriceDto>>(allPrices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting price range for startTicks {StartTicks}, endTicks {EndTicks}, and pair {Pair}",
                    request.StartTicks, request.EndTicks, request.Pair);
                throw;
            }
        }

        private static IEnumerable<long> GetTimePoints(long startTicks, long endTicks, IDictionary<long, Domain.Entities.BitcoinPrice> existingPrices)
        {
            var startTime = ToDateTime(startTicks);
            var endTime = ToDateTime(endTicks);
            var interval = TimeSpan.FromMinutes(60); // 60 minutes intervals
            var current = startTime;

            // Calculate the last needed timestamp (one interval before endTime)
            var lastNeededTimestamp = endTime - interval;

            // Always include the start time
            yield return ToUnixTicks(current);

            // For subsequent points, check if we need them based on what we already have
            while (true)
            {
                var next = current.Add(interval);

                // If next point would be past lastNeededTimestamp, we might need endTime
                if (next > lastNeededTimestamp)
                {
                    // Only include endTime if:
                    // 1. It's exactly on an hourly interval from startTime
                    // 2. We don't have enough prices
                    if ((endTime - startTime).TotalMinutes % 60 == 0 && existingPrices.Count < 2)
                    {
                        yield return ToUnixTicks(endTime);
                    }
                    break;
                }

                // For all other points, always include them
                yield return ToUnixTicks(next);
                current = next;
            }
        }

        /// <summary>
        /// Converts UNIX timestamp (seconds since 1970-01-01 UTC) to UTC DateTime
        /// </summary>
        private static DateTime ToDateTime(long unixTicks)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            return epoch.AddSeconds(unixTicks);
        }

        /// <summary>
        /// Converts UTC DateTime to UNIX timestamp (seconds since 1970-01-01 UTC)
        /// </summary>
        private static long ToUnixTicks(DateTime dateTime)
        {
            var epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            if (dateTime.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("DateTime must be in UTC format", nameof(dateTime));
            }

            return (long)(dateTime - epoch).TotalSeconds;
        }
    }
}