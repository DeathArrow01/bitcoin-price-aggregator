using System.Text.Json;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.ExternalApis.Models;

using Microsoft.Extensions.Logging;

namespace BitcoinPriceAggregator.Infrastructure.ExternalApis
{
    public class BitstampPriceProvider : IPriceProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BitstampPriceProvider> _logger;
        public string ProviderName => "Bitstamp";

        public BitstampPriceProvider(HttpClient httpClient, ILogger<BitstampPriceProvider> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://www.bitstamp.net/api/v2/");
            _logger = logger;
        }

        public async Task<decimal> GetPriceAsync(long utcTicks, string pair)
        {
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(utcTicks);
            var unixTimestamp = utcTicks;
            var formattedPair = pair.ToLower().Replace("/", "");

            try
            {
                var response = await _httpClient.GetAsync($"ohlc/{formattedPair}/?step=3600&limit=1&start={unixTimestamp}");
                response.EnsureSuccessStatusCode();

                var content = await response.Content.ReadAsStringAsync();
                var ohlcData = JsonSerializer.Deserialize<BitstampOhlcResponse>(content);

                if (ohlcData?.Data?.Ohlc == null || ohlcData.Data.Ohlc.Length == 0)
                {
                    _logger.LogError($"No price data available from {ProviderName} for {pair} at {dateTime}");
                }

                return decimal.Parse(ohlcData.Data.Ohlc[0].Close);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get price from Bitstamp for {Pair} at {Timestamp}", pair, dateTime);
                throw;
            }
        }        
    }
} 