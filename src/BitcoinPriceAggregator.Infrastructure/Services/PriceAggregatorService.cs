using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Domain.Exceptions;
using Microsoft.Extensions.Logging;

namespace BitcoinPriceAggregator.Infrastructure.Services
{
    public class PriceAggregatorService : IPriceAggregatorService
    {
        private readonly IEnumerable<IPriceProvider> _priceProviders;
        private readonly ILogger<PriceAggregatorService> _logger;
        private readonly IPriceCalculationStrategy _priceCalculationStrategy;

        public PriceAggregatorService(
            IEnumerable<IPriceProvider> priceProviders,
            ILogger<PriceAggregatorService> logger,
            IPriceCalculationStrategy priceCalculationStrategy)
        {
            _priceProviders = priceProviders ?? throw new ArgumentNullException(nameof(priceProviders));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _priceCalculationStrategy = priceCalculationStrategy ?? throw new ArgumentNullException(nameof(priceCalculationStrategy));
        }

        public async Task<BitcoinPrice> GetAggregatedPriceAsync(long utcTicks, string pair)
        {
            var prices = new List<decimal>();
            var errors = new List<Exception>();

            // Normalize ticks to hour precision
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(utcTicks);
            var normalizedTicks = new DateTimeOffset(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0, 0, dateTime.Offset)
                .ToUnixTimeMilliseconds();

            foreach (var provider in _priceProviders)
            {
                try
                {
                    var price = await provider.GetPriceAsync(normalizedTicks, pair);
                    if (price > 0)
                    {
                        prices.Add(price);
                        _logger.LogDebug("Retrieved price {Price} from provider {Provider}", price, provider.GetType().Name);
                    }
                    else
                    {
                        _logger.LogWarning("Provider {Provider} returned invalid price: {Price}", provider.GetType().Name, price);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to get price from provider {ProviderName}", provider.GetType().Name);
                    errors.Add(ex);
                }
            }

            if (!prices.Any())
            {
                var errorMessage = "Failed to get price from any provider";
                _logger.LogError(errorMessage);
                throw new InvalidOperationException(errorMessage);
            }

            var calculatedPrice = _priceCalculationStrategy.Calculate(prices);
            _logger.LogInformation("Calculated price {Price} from {Count} providers", calculatedPrice, prices.Count);

            return BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(calculatedPrice)
                .WithUtcTicks(normalizedTicks)
                .Build();
        }
    }
} 