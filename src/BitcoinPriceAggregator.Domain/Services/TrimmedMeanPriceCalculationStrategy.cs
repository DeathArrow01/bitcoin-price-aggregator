namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Calculates the trimmed mean price from a list of prices by removing the highest and lowest values
    /// </summary>
    public class TrimmedMeanPriceCalculationStrategy : IPriceCalculationStrategy
    {
        private readonly decimal _trimPercentage;

        /// <summary>
        /// Initializes a new instance of the TrimmedMeanPriceCalculationStrategy
        /// </summary>
        /// <param name="trimPercentage">Percentage of values to trim from each end (0.0 to 0.5)</param>
        public TrimmedMeanPriceCalculationStrategy(decimal trimPercentage = 0.2m)
        {
            if (trimPercentage < 0 || trimPercentage > 0.5m)
            {
                throw new ArgumentException("Trim percentage must be between 0 and 0.5", nameof(trimPercentage));
            }
            _trimPercentage = trimPercentage;
        }

        /// <summary>
        /// Calculates the trimmed mean price from a list of prices
        /// </summary>
        /// <param name="prices">List of prices to calculate trimmed mean from</param>
        /// <returns>The trimmed mean price</returns>
        public decimal Calculate(IEnumerable<decimal> prices)
        {
            if (prices == null || !prices.Any())
            {
                throw new ArgumentException("Prices list cannot be null or empty", nameof(prices));
            }

            var pricesList = prices.OrderBy(p => p).ToList();
            
            if (pricesList.Count <= 2)
            {
                return pricesList.Average();
            }

            int trimCount = (int)Math.Floor(pricesList.Count * _trimPercentage);
            var trimmedPrices = pricesList.Skip(trimCount).Take(pricesList.Count - (2 * trimCount));

            return trimmedPrices.Average();
        }
    }
} 