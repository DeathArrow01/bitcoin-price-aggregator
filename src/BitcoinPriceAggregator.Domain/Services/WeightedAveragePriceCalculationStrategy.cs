namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Calculates the weighted average price from a list of prices, giving more weight to prices closer to the median
    /// </summary>
    public class WeightedAveragePriceCalculationStrategy : IPriceCalculationStrategy
    {
        /// <summary>
        /// Calculates the weighted average price from a list of prices
        /// </summary>
        /// <param name="prices">List of prices to calculate weighted average from</param>
        /// <returns>The weighted average price</returns>
        public decimal Calculate(IEnumerable<decimal> prices)
        {
            if (prices == null || !prices.Any())
            {
                throw new ArgumentException("Prices list cannot be null or empty", nameof(prices));
            }

            var pricesList = prices.ToList();
            var median = new MedianPriceCalculationStrategy().Calculate(pricesList);
            
            // Calculate weights based on distance from median
            var weightedPrices = pricesList.Select(price =>
            {
                var distanceFromMedian = Math.Abs((double)(price - median));
                var weight = 1.0 / (1.0 + distanceFromMedian); // Weight decreases as distance increases
                return (price, weight);
            });

            var totalWeight = weightedPrices.Sum(x => (decimal)x.weight);
            var weightedSum = weightedPrices.Sum(x => x.price * (decimal)x.weight);

            return weightedSum / totalWeight;
        }
    }
} 