namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Calculates the median price from a list of prices
    /// </summary>
    public class MedianPriceCalculationStrategy : IPriceCalculationStrategy
    {
        /// <summary>
        /// Calculates the median price from a list of prices
        /// </summary>
        /// <param name="prices">List of prices to calculate median from</param>
        /// <returns>The median price</returns>
        public decimal Calculate(IEnumerable<decimal> prices)
        {
            if (prices == null || !prices.Any())
            {
                throw new ArgumentException("Prices list cannot be null or empty", nameof(prices));
            }

            var sortedPrices = prices.OrderBy(x => x).ToList();
            var count = sortedPrices.Count;

            if (count % 2 == 0)
            {
                // If even number of prices, take average of middle two
                var lowerMiddle = sortedPrices[(count / 2) - 1];
                var upperMiddle = sortedPrices[count / 2];
                return (lowerMiddle + upperMiddle) / 2;
            }
            else
            {
                // If odd number of prices, take middle value
                return sortedPrices[count / 2];
            }
        }
    }
} 