using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using BitcoinPriceAggregator.Api.Controllers;
using BitcoinPriceAggregator.Application.Common.Models;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;
using BitcoinPriceAggregator.Api.Tests.Fixtures;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net;
using System.Net.Http;

namespace BitcoinPriceAggregator.Api.Tests.Controllers
{
    public class BitcoinPriceControllerUnitTests
    {
        private readonly Mock<IMediator> _mediatorMock;
        private readonly Mock<ILogger<BitcoinPriceController>> _mockLogger;
        private readonly BitcoinPriceController _controller;
        private readonly Fixture _fixture;

        public BitcoinPriceControllerUnitTests()
        {
            _mediatorMock = new Mock<IMediator>();
            _mockLogger = new Mock<ILogger<BitcoinPriceController>>();
            _controller = new BitcoinPriceController(_mediatorMock.Object, _mockLogger.Object);
            _fixture = new Fixture();
        }

        [Fact]
        public async Task GetPrice_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow;
            var pair = "BTC/USD";
            var expectedPrice = new BitcoinPriceDto { Pair = pair, Price = 42000.00m, Timestamp = timestamp };

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetAggregatedPriceQuery>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(expectedPrice));

            // Act
            var result = await _controller.GetPrice(timestamp, pair);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<BitcoinPriceDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be(expectedPrice);
        }

        [Fact]
        public async Task GetPriceRange_WithValidRequest_ReturnsOkResult()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow.AddHours(-1);
            var endTime = DateTimeOffset.UtcNow;
            var pair = "BTC/USD";
            var prices = new List<BitcoinPriceDto>
            {
                new BitcoinPriceDto { Timestamp = startTime, Pair = pair, Price = 50000m },
                new BitcoinPriceDto { Timestamp = endTime, Pair = pair, Price = 51000m }
            };

            _mediatorMock
                .Setup(x => x.Send(It.IsAny<GetPriceRangeQuery>(), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<BitcoinPriceDto>>(prices));

            // Act
            var result = await _controller.GetPriceRange(startTime, endTime, pair);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<BitcoinPriceDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeEquivalentTo(prices);
        }

        [Fact]
        public async Task GetPrice_WithDefaultPair_UsesCorrectPair()
        {
            // Arrange
            var timestamp = DateTimeOffset.UtcNow;
            var expectedPrice = new BitcoinPriceDto { Pair = "BTC/USD", Price = 42000.00m, Timestamp = timestamp };

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetAggregatedPriceQuery>(q => q.Pair == "BTC/USD"), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult(expectedPrice));

            // Act
            var result = await _controller.GetPrice(timestamp);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<BitcoinPriceDto>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().Be(expectedPrice);
        }

        [Fact]
        public async Task GetPriceRange_WithDefaultPair_UsesCorrectPair()
        {
            // Arrange
            var startTime = DateTimeOffset.UtcNow.AddHours(-1);
            var endTime = DateTimeOffset.UtcNow;
            var prices = new List<BitcoinPriceDto>
            {
                new BitcoinPriceDto { Timestamp = startTime, Pair = "BTC/USD", Price = 50000m },
                new BitcoinPriceDto { Timestamp = endTime, Pair = "BTC/USD", Price = 51000m }
            };

            _mediatorMock
                .Setup(x => x.Send(It.Is<GetPriceRangeQuery>(q => q.Pair == "BTC/USD"), It.IsAny<CancellationToken>()))
                .Returns(Task.FromResult<IEnumerable<BitcoinPriceDto>>(prices));

            // Act
            var result = await _controller.GetPriceRange(startTime, endTime);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            var response = okResult.Value.Should().BeOfType<ApiResponse<IEnumerable<BitcoinPriceDto>>>().Subject;
            response.Success.Should().BeTrue();
            response.Data.Should().BeEquivalentTo(prices);
        }
    }
} 