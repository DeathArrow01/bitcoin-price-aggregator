using Serilog;
using Serilog.Events;

namespace BitcoinPriceAggregator.Api.Configuration
{
    /// <summary>
    /// Configuration class for logging setup
    /// </summary>
    public static class LoggingConfiguration
    {
        /// <summary>
        /// Configures and creates the Serilog logger
        /// </summary>
        public static Serilog.ILogger CreateLogger()
        {
            return new LoggerConfiguration()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                .MinimumLevel.Override("System", LogEventLevel.Warning)
                .Enrich.FromLogContext()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(
                    outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}")
                .CreateLogger();
        }

        /// <summary>
        /// Configures Serilog for the web host
        /// </summary>
        public static IHostBuilder AddSerilogConfiguration(this IHostBuilder builder)
        {
            return builder.UseSerilog();
        }
    }
} 