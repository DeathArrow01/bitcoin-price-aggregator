namespace BitcoinPriceAggregator.Application.Configuration
{
    public class RetryPolicySettings
    {
        public int TotalRetries { get; set; }
        public int ImmediateRetries { get; set; }
        public int BaseDelayInSeconds { get; set; }
    }
} 