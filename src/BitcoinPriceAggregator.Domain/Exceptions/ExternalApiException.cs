using System;

namespace BitcoinPriceAggregator.Domain.Exceptions
{
    public class ExternalApiException : BitcoinPriceException
    {
        public string Provider { get; }

        public ExternalApiException(string provider, string message) 
            : base($"Provider {provider}: {message}")
        {
            Provider = provider;
        }

        public ExternalApiException(string provider, string message, Exception inner) 
            : base($"Provider {provider}: {message}", inner)
        {
            Provider = provider;
        }
    }
} 