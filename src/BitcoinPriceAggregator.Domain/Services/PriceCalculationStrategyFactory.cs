namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Factory for creating price calculation strategies
    /// </summary>
    public interface IPriceCalculationStrategyFactory
    {
        /// <summary>
        /// Creates a price calculation strategy based on the strategy name
        /// </summary>
        /// <param name="strategyName">Name of the strategy to create</param>
        /// <returns>The requested price calculation strategy</returns>
        IPriceCalculationStrategy CreateStrategy(string strategyName);
    }

    /// <summary>
    /// Implementation of the price calculation strategy factory
    /// </summary>
    public class PriceCalculationStrategyFactory : IPriceCalculationStrategyFactory
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        /// Initializes a new instance of the PriceCalculationStrategyFactory
        /// </summary>
        /// <param name="serviceProvider">Service provider for resolving strategy instances</param>
        public PriceCalculationStrategyFactory(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        /// <summary>
        /// Available strategy names
        /// </summary>
        public static class StrategyNames
        {
            public const string Average = "Average";
            public const string Median = "Median";
            public const string WeightedAverage = "WeightedAverage";
            public const string TrimmedMean = "TrimmedMean";
        }

        /// <summary>
        /// Creates a price calculation strategy based on the strategy name
        /// </summary>
        /// <param name="strategyName">Name of the strategy to create</param>
        /// <returns>The requested price calculation strategy</returns>
        public IPriceCalculationStrategy CreateStrategy(string strategyName)
        {
            if (string.IsNullOrWhiteSpace(strategyName))
            {
                return _serviceProvider.GetService(typeof(AveragePriceCalculationStrategy)) as IPriceCalculationStrategy
                    ?? new AveragePriceCalculationStrategy();
            }

            return strategyName.ToLowerInvariant() switch
            {
                var s when s == StrategyNames.Average.ToLowerInvariant() =>
                    _serviceProvider.GetService(typeof(AveragePriceCalculationStrategy)) as IPriceCalculationStrategy
                    ?? new AveragePriceCalculationStrategy(),

                var s when s == StrategyNames.Median.ToLowerInvariant() =>
                    _serviceProvider.GetService(typeof(MedianPriceCalculationStrategy)) as IPriceCalculationStrategy
                    ?? new MedianPriceCalculationStrategy(),

                var s when s == StrategyNames.WeightedAverage.ToLowerInvariant() =>
                    _serviceProvider.GetService(typeof(WeightedAveragePriceCalculationStrategy)) as IPriceCalculationStrategy
                    ?? new WeightedAveragePriceCalculationStrategy(),

                var s when s == StrategyNames.TrimmedMean.ToLowerInvariant() =>
                    _serviceProvider.GetService(typeof(TrimmedMeanPriceCalculationStrategy)) as IPriceCalculationStrategy
                    ?? new TrimmedMeanPriceCalculationStrategy(),

                _ => throw new ArgumentException($"Unknown strategy name: {strategyName}", nameof(strategyName))
            };
        }
    }
} 