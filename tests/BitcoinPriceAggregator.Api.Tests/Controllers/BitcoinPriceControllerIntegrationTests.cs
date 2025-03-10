using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using BitcoinPriceAggregator.Api.Tests.Fixtures;
using BitcoinPriceAggregator.Application.Common.Models;
using BitcoinPriceAggregator.Application.DTOs;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace BitcoinPriceAggregator.Api.Tests.Controllers
{
    public class BitcoinPriceControllerIntegrationTests : IClassFixture<WebApplicationFactoryFixture>, IDisposable
    {
        private readonly WebApplicationFactoryFixture _factory;
        private readonly HttpClient _client;

        public BitcoinPriceControllerIntegrationTests(WebApplicationFactoryFixture factory)
        {
            _factory = factory ?? throw new ArgumentNullException(nameof(factory));
            _client = _factory.CreateClient();
        }

        public void Dispose()
        {
            _client?.Dispose();
        }

        [Fact]
        public async Task GetPrice_ReturnsOkResult()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow;

            // Act
            var response = await _client.GetAsync($"/api/v1/bitcoin-price/price/{timestamp:o}");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<BitcoinPriceDto>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data!.Pair.Should().Be("BTC/USD");
            result.Data.Price.Should().Be(42000.00m);
            result.Data.Timestamp.Should().BeCloseTo(timestamp, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetPriceRange_ReturnsOkResult()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow.AddHours(-1);
            var endTime = DateTimeOffset.UtcNow;

            // Act
            var response = await _client.GetAsync($"/api/v1/bitcoin-price/price-range?startTime={startTime:o}&endTime={endTime:o}");

            // Assert
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<BitcoinPriceDto[]>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var prices = result.Data!;
            prices[0].Price.Should().Be(42000.00m);
            prices[1].Price.Should().Be(43000.00m);
            prices[0].Timestamp.Should().BeCloseTo(startTime, TimeSpan.FromSeconds(1));
            prices[1].Timestamp.Should().BeCloseTo(endTime, TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task GetPrice_WithInvalidPair_ReturnsBadRequest()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow;

            // Act
            var response = await _client.GetAsync($"/api/v1/bitcoin-price/price/{timestamp:o}?pair=INVALID");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Trading pair must be in format 'XXX/YYY'");
        }

        [Fact]
        public async Task GetPriceRange_WithInvalidTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow;
            var endTime = startTime.AddHours(-1); // End time before start time

            // Act
            var response = await _client.GetAsync($"/api/v1/bitcoin-price/price-range?startTime={startTime:o}&endTime={endTime:o}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("End time must be after start time");
        }

        [Fact]
        public async Task GetPriceRange_WithExcessiveTimeRange_ReturnsBadRequest()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow.AddDays(-31); // More than 30 days
            var endTime = DateTimeOffset.UtcNow;

            // Act
            var response = await _client.GetAsync($"/api/v1/bitcoin-price/price-range?startTime={startTime:o}&endTime={endTime:o}");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Time range cannot exceed 30 days");
        }
    }
} 