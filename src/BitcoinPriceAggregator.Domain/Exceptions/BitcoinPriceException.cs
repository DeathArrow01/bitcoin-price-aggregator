using System;

namespace BitcoinPriceAggregator.Domain.Exceptions
{
    public class BitcoinPriceException : Exception
    {
        public BitcoinPriceException() { }
        public BitcoinPriceException(string message) : base(message) { }
        public BitcoinPriceException(string message, Exception inner) : base(message, inner) { }
    }
} 