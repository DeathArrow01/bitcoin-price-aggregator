namespace BitcoinPriceAggregator.Api.Settings;

public class ExternalApiSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 30;
    public string ApiKey { get; set; } = string.Empty;
} 