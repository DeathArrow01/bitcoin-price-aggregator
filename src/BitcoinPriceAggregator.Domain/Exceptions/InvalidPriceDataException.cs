using System;

namespace BitcoinPriceAggregator.Domain.Exceptions
{
    public class InvalidPriceDataException : BitcoinPriceException
    {
        public string Pair { get; }
        public DateTime Timestamp { get; }

        public InvalidPriceDataException(string pair, DateTime timestamp, string message) 
            : base($"Invalid price data for {pair} at {timestamp:u}: {message}")
        {
            Pair = pair;
            Timestamp = timestamp;
        }
    }
} 