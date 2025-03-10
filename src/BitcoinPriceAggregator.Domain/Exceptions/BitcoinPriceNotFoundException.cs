namespace BitcoinPriceAggregator.Domain.Exceptions
{
    /// <summary>
    /// Exception thrown when bitcoin price data is not found
    /// </summary>
    public class BitcoinPriceNotFoundException : DomainException
    {
        public BitcoinPriceNotFoundException(string pair, DateTime timestamp) 
            : base($"Bitcoin price not found for pair {pair} at timestamp {timestamp:yyyy-MM-dd HH:mm:ss}")
        {
            Pair = pair;
            Timestamp = timestamp;
        }

        public string Pair { get; }
        public DateTime Timestamp { get; }
    }
} 