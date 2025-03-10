namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Calculates the average price from a list of prices
    /// </summary>
    public class AveragePriceCalculationStrategy : IPriceCalculationStrategy
    {
        /// <summary>
        /// Calculates the average price from a list of prices
        /// </summary>
        /// <param name="prices">List of prices to average</param>
        /// <returns>The average price</returns>
        public decimal Calculate(IEnumerable<decimal> prices)
        {
            if (prices == null || !prices.Any())
            {
                throw new ArgumentException("Prices list cannot be null or empty", nameof(prices));
            }

            return prices.Average();
        }
    }
} 