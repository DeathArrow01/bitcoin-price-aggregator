using System;
using System.Threading;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BitcoinPriceAggregator.Application.BackgroundServices
{
    public class CachePrimingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<CachePrimingService> _logger;
        private readonly TimeSpan _interval;

        public CachePrimingService(
            IServiceScopeFactory scopeFactory,
            ILogger<CachePrimingService> logger,
            TimeSpan interval)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting cache priming");

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IBitcoinPriceRepository>();
                        var aggregatorService = scope.ServiceProvider.GetRequiredService<IPriceAggregatorService>();

                        var now = DateTimeOffset.UtcNow;
                        var utcTicks = now.ToUnixTimeMilliseconds();
                        var pair = "BTC/USD";

                        var price = await repository.GetPriceAsync(utcTicks, pair);
                        if (price == null)
                        {
                            var aggregatedPrice = await aggregatorService.GetAggregatedPriceAsync(utcTicks, pair);
                            if (aggregatedPrice != null)
                            {
                                await repository.StorePriceAsync(aggregatedPrice);
                                _logger.LogInformation("Primed cache with new price for {Pair} at {Time}", pair, now);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during cache priming");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}