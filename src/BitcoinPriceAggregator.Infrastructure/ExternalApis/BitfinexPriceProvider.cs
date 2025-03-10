using System.Net.Http.Json;
using BitcoinPriceAggregator.Domain.Services;
using Microsoft.Extensions.Logging;

namespace BitcoinPriceAggregator.Infrastructure.ExternalApis
{
    public class BitfinexPriceProvider : IPriceProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BitfinexPriceProvider> _logger;
        public string ProviderName => "Bitfinex";

        public BitfinexPriceProvider(HttpClient httpClient, ILogger<BitfinexPriceProvider> logger)
        {
            _httpClient = httpClient;
            _httpClient.BaseAddress = new Uri("https://api.bitfinex.com/v2/");
            _logger = logger;
        }

        public async Task<decimal> GetPriceAsync(long utcTicks, string pair)
        {
            var dateTime = DateTimeOffset.FromUnixTimeMilliseconds(utcTicks);
            var unixTimestamp = utcTicks;
            var endTimestamp = unixTimestamp + TimeSpan.FromHours(1).TotalMilliseconds;

            var url = $"candles/trade:1h:t{pair.Replace("/", "")}/hist?start={unixTimestamp}&end={endTimestamp}&limit=1";
            try
            {
                var response = await _httpClient.GetFromJsonAsync<decimal[][]>(url);
                if (response == null || response.Length == 0)
                {
                    _logger.LogWarning("No data returned from Bitfinex for {Pair} at {Timestamp}", pair, dateTime);
                    return 0;
                }

                var closePrice = response[0][2]; // Close price is at index 2
                _logger.LogDebug("Retrieved price {Price} from Bitfinex for {Pair} at {Timestamp}", closePrice, pair, dateTime);
                return closePrice;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get price from Bitfinex for {Pair} at {Timestamp}", pair, dateTime);
                throw;
            }
        }
    }
} 