namespace BitcoinPriceAggregator.Domain.Services
{
    /// <summary>
    /// Interface for implementing different price calculation strategies
    /// </summary>
    public interface IPriceCalculationStrategy
    {
        /// <summary>
        /// Calculates a single price from a list of prices
        /// </summary>
        /// <param name="prices">List of prices to aggregate</param>
        /// <returns>The calculated price</returns>
        decimal Calculate(IEnumerable<decimal> prices);
    }
} 