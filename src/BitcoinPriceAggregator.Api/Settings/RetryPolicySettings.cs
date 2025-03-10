namespace BitcoinPriceAggregator.Api.Settings;

public class RetryPolicySettings
{
    public int TotalRetries { get; set; } = 3;
    public int ImmediateRetries { get; set; } = 1;
    public int BaseDelayInSeconds { get; set; } = 2;
    public bool EnableRetry { get; set; } = true;
} 