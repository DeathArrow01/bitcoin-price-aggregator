using System;
using System.Threading;
using System.Threading.Tasks;
using AutoFixture;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Domain.Exceptions;
using Microsoft.Extensions.Logging;
using FluentValidation;
using FluentValidation.Results;
using BitcoinPriceAggregator.Application.Queries.Validators;

namespace BitcoinPriceAggregator.Application.Tests.Queries
{
    public class GetAggregatedPriceQueryHandlerTests
    {
        private readonly Mock<IBitcoinPriceRepository> _mockRepository;
        private readonly Mock<IPriceAggregatorService> _mockAggregatorService;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<GetAggregatedPriceQueryHandler>> _mockLogger;
        private readonly Mock<IValidator<GetAggregatedPriceQuery>> _mockValidator;
        private readonly Fixture _fixture;
        private readonly GetAggregatedPriceQueryHandler _handler;

        public GetAggregatedPriceQueryHandlerTests()
        {
            _mockRepository = new Mock<IBitcoinPriceRepository>();
            _mockAggregatorService = new Mock<IPriceAggregatorService>();
            _mapperMock = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<GetAggregatedPriceQueryHandler>>();
            _mockValidator = new Mock<IValidator<GetAggregatedPriceQuery>>();
            _fixture = new Fixture();

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<GetAggregatedPriceQuery>(), default))
                .ReturnsAsync((GetAggregatedPriceQuery query, CancellationToken _) =>
                {
                    var result = new ValidationResult();
                    if (query.Pair == "invalid-pair")
                    {
                        result.Errors.Add(new ValidationFailure("Pair", "Invalid pair format"));
                    }
                    return result;
                });

            _handler = new GetAggregatedPriceQueryHandler(
                _mockRepository.Object,
                _mockAggregatorService.Object,
                _mapperMock.Object,
                _mockLogger.Object,
                _mockValidator.Object);
        }

        [Fact]
        public async Task Handle_WithCachedPrice_ReturnsCachedPrice()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var normalizedTicks = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc).Ticks;
            var pair = "BTC/USD";
            var cachedPrice = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(45000.50m)
                .WithUtcTicks(normalizedTicks)
                .Build();

            var expectedDto = new BitcoinPriceDto
            {
                Pair = pair,
                Price = 45000.50m,
                Timestamp = new DateTime(normalizedTicks, DateTimeKind.Utc)
            };

            _mockRepository.Setup(r => r.GetPriceAsync(normalizedTicks, pair, default))
                .ReturnsAsync(cachedPrice);

            _mapperMock.Setup(m => m.Map<BitcoinPriceDto>(cachedPrice))
                .Returns(expectedDto);

            var query = new GetAggregatedPriceQuery(normalizedTicks, pair);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);
            _mockAggregatorService.Verify(s => s.GetAggregatedPriceAsync(normalizedTicks, pair), Times.Never);
        }

        [Fact]
        public async Task Handle_WithoutCachedPrice_GetsAndStoresAggregatedPrice()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var normalizedTicks = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc).Ticks;
            var pair = "BTC/USD";
            var aggregatedPrice = BitcoinPrice.CreateBuilder()
                .WithPair(pair)
                .WithPrice(45000.50m)
                .WithUtcTicks(normalizedTicks)
                .Build();

            var expectedDto = new BitcoinPriceDto
            {
                Pair = pair,
                Price = 45000.50m,
                Timestamp = new DateTime(normalizedTicks, DateTimeKind.Utc)
            };

            _mockRepository.Setup(r => r.GetPriceAsync(normalizedTicks, pair, default))
                .ReturnsAsync((BitcoinPrice)null);

            _mockAggregatorService.Setup(s => s.GetAggregatedPriceAsync(normalizedTicks, pair))
                .ReturnsAsync(aggregatedPrice);

            _mapperMock.Setup(m => m.Map<BitcoinPriceDto>(aggregatedPrice))
                .Returns(expectedDto);

            var query = new GetAggregatedPriceQuery(normalizedTicks, pair);

            // Act
            var result = await _handler.Handle(query, CancellationToken.None);

            // Assert
            result.Should().NotBeNull();
            result.Should().BeEquivalentTo(expectedDto);
            _mockRepository.Verify(r => r.StorePriceAsync(aggregatedPrice, default), Times.Once);
        }

        [Fact]
        public async Task Handle_WithInvalidPair_ThrowsValidationException()
        {
            // Arrange
            var timestamp = DateTime.UtcNow;
            var normalizedTicks = new DateTime(timestamp.Year, timestamp.Month, timestamp.Day, timestamp.Hour, 0, 0, DateTimeKind.Utc).Ticks;
            var query = new GetAggregatedPriceQuery(normalizedTicks, "invalid-pair");

            // Act & Assert
            await _handler.Invoking(h => h.Handle(query, CancellationToken.None))
                .Should().ThrowAsync<ValidationException>();
        }

        [Theory]
        [InlineData(true, false, false, false, false, "repository")]
        [InlineData(false, true, false, false, false, "aggregatorService")]
        [InlineData(false, false, true, false, false, "mapper")]
        [InlineData(false, false, false, true, false, "logger")]
        [InlineData(false, false, false, false, true, "validator")]
        public void Constructor_WithNullDependencies_ThrowsArgumentNullException(
            bool isRepositoryNull,
            bool isAggregatorServiceNull,
            bool isMapperNull,
            bool isLoggerNull,
            bool isValidatorNull,
            string expectedParamName)
        {
            // Arrange
            var repository = isRepositoryNull ? null : _mockRepository.Object;
            var aggregatorService = isAggregatorServiceNull ? null : _mockAggregatorService.Object;
            var mapper = isMapperNull ? null : _mapperMock.Object;
            var logger = isLoggerNull ? null : _mockLogger.Object;
            var validator = isValidatorNull ? null : _mockValidator.Object;

            // Act & Assert
            var exception = Assert.Throws<ArgumentNullException>(() =>
                new GetAggregatedPriceQueryHandler(repository!, aggregatorService!, mapper!, logger!, validator!));

            exception.ParamName.Should().Be(expectedParamName);
        }
    }
} 