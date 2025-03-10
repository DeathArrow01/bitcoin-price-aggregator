namespace BitcoinPriceAggregator.Api.Settings;

public class BackgroundServiceSettings
{
    public CachePrimingSettings CachePriming { get; set; } = new();
    public DatabaseMaintenanceSettings DatabaseMaintenance { get; set; } = new();
}

public class CachePrimingSettings
{
    public int IntervalMinutes { get; set; } = 5;
}

public class DatabaseMaintenanceSettings
{
    public int IntervalHours { get; set; } = 24;
    public int RetentionDays { get; set; } = 30;
} 