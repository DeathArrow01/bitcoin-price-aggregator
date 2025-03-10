using AutoMapper;
using BitcoinPriceAggregator.Api.Middleware;
using BitcoinPriceAggregator.Application.Common.Models;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Application.Mapping;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Application.Queries.Validators;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.Persistence;
using FluentAssertions;
using FluentValidation;
using FluentValidation.AspNetCore;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Moq;
using Polly;
using Polly.Extensions.Http;
using System.IO.MemoryMappedFiles;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using BitcoinPriceAggregator.Api.Configuration;
using BitcoinPriceAggregator.Api.Controllers;
using Xunit;
using Newtonsoft.Json;
using BitcoinPriceAggregator.Api.Tests.Fixtures;

namespace BitcoinPriceAggregator.Api.Tests.IntegrationTests
{
    public class BitcoinPriceControllerTests : IClassFixture<WebApplicationFactoryFixture>, IDisposable
    {
        private readonly WebApplicationFactoryFixture _factory;
        private readonly HttpClient _client;

        public BitcoinPriceControllerTests(WebApplicationFactoryFixture factory)
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
            var result = JsonConvert.DeserializeObject<ApiResponse<BitcoinPriceDto>>(content);

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
            var result = JsonConvert.DeserializeObject<ApiResponse<IEnumerable<BitcoinPriceDto>>>(content);

            result.Should().NotBeNull();
            result!.Success.Should().BeTrue();
            result.Data.Should().NotBeNull();
            result.Data.Should().HaveCount(2);

            var prices = result.Data!.ToList();
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
            var result = JsonConvert.DeserializeObject<ApiResponse<object>>(content);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
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
            var result = JsonConvert.DeserializeObject<ApiResponse<object>>(content);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().NotBeNullOrEmpty();
        }
    }
}