using System;
using System.Threading;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Domain.Repositories;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

namespace BitcoinPriceAggregator.Application.BackgroundServices
{
    public class DatabaseMaintenanceService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<DatabaseMaintenanceService> _logger;
        private readonly TimeSpan _retentionPeriod;
        private readonly TimeSpan _interval;

        public DatabaseMaintenanceService(
            IServiceScopeFactory scopeFactory,
            ILogger<DatabaseMaintenanceService> logger,
            TimeSpan retentionPeriod,
            TimeSpan interval)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
            _retentionPeriod = retentionPeriod;
            _interval = interval;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Starting database maintenance");

                    using (var scope = _scopeFactory.CreateScope())
                    {
                        var repository = scope.ServiceProvider.GetRequiredService<IBitcoinPriceRepository>();

                        var cutoffTime = DateTimeOffset.UtcNow.AddDays(-_retentionPeriod.TotalDays);
                        var cutoffTicks = cutoffTime.Ticks;
                        await repository.RemoveOldDataAsync(cutoffTicks);

                        _logger.LogInformation("Removed old price records older than {CutoffTime}", cutoffTime);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error during database maintenance");
                }

                await Task.Delay(_interval, stoppingToken);
            }
        }
    }
}