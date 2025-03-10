using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AutoMapper;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Application.Queries.Validators;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;

using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BitcoinPriceAggregator.Application.Tests.Queries
{
    public class GetPriceRangeQueryHandlerTests
    {
        private readonly Mock<IBitcoinPriceRepository> _mockRepository;
        private readonly Mock<IPriceAggregatorService> _mockAggregatorService;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IValidator<GetPriceRangeQuery>> _mockValidator;
        private readonly Mock<ILogger<GetPriceRangeQueryHandler>> _mockLogger;
        private readonly GetPriceRangeQueryHandler _handler;

        public GetPriceRangeQueryHandlerTests()
        {
            _mockRepository = new Mock<IBitcoinPriceRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockValidator = new Mock<IValidator<GetPriceRangeQuery>>();
            _mockAggregatorService = new Mock<IPriceAggregatorService>();
            _mockLogger = new Mock<ILogger<GetPriceRangeQueryHandler>>();
            

            _mockValidator.Setup(v => v.ValidateAsync(It.IsAny<GetPriceRangeQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult());

            _handler = new GetPriceRangeQueryHandler(
                _mockRepository.Object,
                _mockAggregatorService.Object,
                _mockMapper.Object,
                _mockValidator.Object,
                _mockLogger.Object);
        }
      

        [Fact]
        public async Task Handle_WithInvalidRequest_ThrowsValidationException()
        {
            // Arrange
            var query = new GetPriceRangeQuery
            {
                StartTicks = DateTimeOffset.UtcNow.Ticks,
                EndTicks = DateTimeOffset.UtcNow.AddHours(-1).Ticks,
                Pair = "BTC/USD"
            };

            var validationFailures = new List<ValidationFailure>
            {
                new ValidationFailure("EndTicks", "End time must be after start time")
            };

            _mockValidator.Setup(v => v.ValidateAsync(It.Is<GetPriceRangeQuery>(x => x == query), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new ValidationResult(validationFailures));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => _handler.Handle(query, CancellationToken.None));
            _mockRepository.Verify(r => r.GetPriceRangeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Handle_WhenRepositoryThrows_LogsAndRethrows()
        {
            // Arrange
            var query = new GetPriceRangeQuery
            {
                StartTicks = DateTimeOffset.UtcNow.AddHours(-1).Ticks,
                EndTicks = DateTimeOffset.UtcNow.Ticks,
                Pair = "BTC/USD"
            };

            var exception = new Exception("Repository error");
            _mockRepository.Setup(r => r.GetPriceRangeAsync(It.Is<long>(x => x == query.StartTicks), It.Is<long>(x => x == query.EndTicks), It.Is<string>(x => x == query.Pair), It.IsAny<CancellationToken>()))
                .ThrowsAsync(exception);

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() => _handler.Handle(query, CancellationToken.None));
            _mockLogger.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    exception,
                    It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
                Times.Once);
        }

        [Fact]
        public void Constructor_WithNullParameters_ThrowsArgumentNullException()
        {
            // Arrange & Act & Assert
            Assert.Throws<ArgumentNullException>(() => new GetPriceRangeQueryHandler(null!,_mockAggregatorService.Object, _mockMapper.Object, _mockValidator.Object, _mockLogger.Object));
            Assert.Throws<ArgumentNullException>(() => new GetPriceRangeQueryHandler(_mockRepository.Object, null!, _mockMapper.Object, _mockValidator.Object, _mockLogger.Object));
            Assert.Throws<ArgumentNullException>(() => new GetPriceRangeQueryHandler(_mockRepository.Object, _mockAggregatorService.Object, null!, _mockValidator.Object, _mockLogger.Object));
            Assert.Throws<ArgumentNullException>(() => new GetPriceRangeQueryHandler(_mockRepository.Object, _mockAggregatorService.Object, _mockMapper.Object, null!, _mockLogger.Object));
            Assert.Throws<ArgumentNullException>(() => new GetPriceRangeQueryHandler(_mockRepository.Object, _mockAggregatorService.Object, _mockMapper.Object, _mockValidator.Object, null!));
        }
    }
}