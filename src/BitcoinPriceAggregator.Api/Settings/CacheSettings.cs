namespace BitcoinPriceAggregator.Api.Settings;

public class CacheSettings
{
    public int ExpirationMinutes { get; set; } = 60;
    public bool EnableCaching { get; set; } = true;
} 