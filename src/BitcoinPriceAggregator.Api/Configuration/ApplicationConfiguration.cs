using BitcoinPriceAggregator.Api.Settings;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.ExternalApis;
using BitcoinPriceAggregator.Infrastructure.Persistence;
using BitcoinPriceAggregator.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Polly;
using Polly.Extensions.Http;
using BitcoinPriceAggregator.Application.BackgroundServices;

namespace BitcoinPriceAggregator.Api.Configuration
{
    /// <summary>
    /// Configuration class for application settings and services
    /// </summary>
    public static class ApplicationConfiguration
    {
        /// <summary>
        /// Configures application settings and services
        /// </summary>
        public static IServiceCollection AddApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Configure settings
            services.Configure<CacheSettings>(
                configuration.GetSection("CacheSettings"));
            services.Configure<RetryPolicySettings>(
                configuration.GetSection("RetryPolicySettings"));
            services.Configure<ExternalApiSettings>(
                configuration.GetSection("ExternalApiSettings"));
            services.Configure<BackgroundServiceSettings>(
                configuration.GetSection("BackgroundServices"));

            // Configure SQLite
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlite(configuration.GetConnectionString("DefaultConnection")));

            // Configure Memory Cache
            services.AddMemoryCache();

            // Configure HTTP Clients with Polly
            ConfigureHttpClients(services, configuration);

            // Register Services
            services.AddScoped<IBitcoinPriceRepository, BitcoinPriceRepository>();
            services.AddScoped<IPriceAggregatorService, PriceAggregatorService>();
            
            // Register Price Calculation Strategies
            services.AddScoped<IPriceCalculationStrategy, AveragePriceCalculationStrategy>();
            services.AddScoped<AveragePriceCalculationStrategy>();
            services.AddScoped<MedianPriceCalculationStrategy>();
            services.AddScoped<WeightedAveragePriceCalculationStrategy>();
            services.AddScoped<TrimmedMeanPriceCalculationStrategy>();
            services.AddScoped<IPriceCalculationStrategyFactory, PriceCalculationStrategyFactory>();

            // Register Background Services
            ConfigureBackgroundServices(services, configuration);

            // Add Health Checks
            services.AddHealthChecks()
                .AddDbContextCheck<ApplicationDbContext>();

            return services;
        }

        private static void ConfigureHttpClients(IServiceCollection services, IConfiguration configuration)
        {
            var retrySettings = configuration
                .GetSection("RetryPolicySettings")
                .Get<RetryPolicySettings>();

            var retryPolicy = HttpPolicyExtensions
                .HandleTransientHttpError()
                .WaitAndRetryAsync(
                    retrySettings?.TotalRetries ?? 3,
                    retryAttempt => retryAttempt <= (retrySettings?.ImmediateRetries ?? 1)
                        ? TimeSpan.Zero
                        : TimeSpan.FromSeconds(Math.Pow(retrySettings?.BaseDelayInSeconds ?? 2, retryAttempt - (retrySettings?.ImmediateRetries ?? 1))));

            services.AddHttpClient<IPriceProvider, BitstampPriceProvider>()
                .AddPolicyHandler(retryPolicy);

            services.AddHttpClient<IPriceProvider, BitfinexPriceProvider>()
                .AddPolicyHandler(retryPolicy);
        }

        private static void ConfigureBackgroundServices(IServiceCollection services, IConfiguration configuration)
        {
            var backgroundSettings = configuration
                .GetSection("BackgroundServices")
                .Get<BackgroundServiceSettings>() ?? new BackgroundServiceSettings();

            // Register CachePrimingService with a service scope factory
            services.AddSingleton<IHostedService>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILogger<CachePrimingService>>();
                var interval = TimeSpan.FromMinutes(backgroundSettings.CachePriming.IntervalMinutes);

                return new CachePrimingService(scopeFactory, logger, interval);
            });

            // Register DatabaseMaintenanceService with a service scope factory
            services.AddSingleton<IHostedService>(sp =>
            {
                var scopeFactory = sp.GetRequiredService<IServiceScopeFactory>();
                var logger = sp.GetRequiredService<ILogger<DatabaseMaintenanceService>>();
                var interval = TimeSpan.FromHours(backgroundSettings.DatabaseMaintenance.IntervalHours);
                var retentionPeriod = TimeSpan.FromDays(backgroundSettings.DatabaseMaintenance.RetentionDays);

                return new DatabaseMaintenanceService(scopeFactory, logger, retentionPeriod, interval);
            });
        }
    }
} 