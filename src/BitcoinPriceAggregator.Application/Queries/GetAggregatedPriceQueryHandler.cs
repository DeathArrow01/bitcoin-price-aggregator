using System;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using MediatR;
using Microsoft.Extensions.Logging;
using FluentValidation;

namespace BitcoinPriceAggregator.Application.Queries
{
    public class GetAggregatedPriceQueryHandler : IRequestHandler<GetAggregatedPriceQuery, BitcoinPriceDto>
    {
        private readonly IBitcoinPriceRepository _repository;
        private readonly IPriceAggregatorService _aggregatorService;
        private readonly IMapper _mapper;
        private readonly ILogger<GetAggregatedPriceQueryHandler> _logger;
        private readonly IValidator<GetAggregatedPriceQuery> _validator;

        public GetAggregatedPriceQueryHandler(
            IBitcoinPriceRepository repository,
            IPriceAggregatorService aggregatorService,
            IMapper mapper,
            ILogger<GetAggregatedPriceQueryHandler> logger,
            IValidator<GetAggregatedPriceQuery> validator)
        {
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
            _aggregatorService = aggregatorService ?? throw new ArgumentNullException(nameof(aggregatorService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        }

        public async Task<BitcoinPriceDto> Handle(GetAggregatedPriceQuery request, CancellationToken cancellationToken)
        {
            var validationResult = await _validator.ValidateAsync(request, cancellationToken);
            if (!validationResult.IsValid)
            {
                throw new ValidationException(validationResult.Errors);
            }

            var cachedPrice = await _repository.GetPriceAsync(request.UtcTicks, request.Pair);
            if (cachedPrice != null)
            {
                _logger.LogInformation("Retrieved cached price for {Pair} at {Time}", request.Pair, new DateTimeOffset(request.UtcTicks, TimeSpan.Zero));
                return _mapper.Map<BitcoinPriceDto>(cachedPrice);
            }

            var aggregatedPrice = await _aggregatorService.GetAggregatedPriceAsync(request.UtcTicks, request.Pair);
            if (aggregatedPrice != null)
            {
                await _repository.StorePriceAsync(aggregatedPrice);
                _logger.LogInformation("Stored new price for {Pair} at {Time}", request.Pair, new DateTimeOffset(request.UtcTicks, TimeSpan.Zero));
                return _mapper.Map<BitcoinPriceDto>(aggregatedPrice);
            }

            return null;
        }
    }
} 