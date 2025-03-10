namespace BitcoinPriceAggregator.Application.Configuration
{
    public class ExternalApiSettings
    {
        public ApiConfig Bitstamp { get; set; } = new();
        public ApiConfig Bitfinex { get; set; } = new();
    }

    public class ApiConfig
    {
        public string BaseUrl { get; set; } = string.Empty;
        public int TimeoutInSeconds { get; set; }
    }
} 