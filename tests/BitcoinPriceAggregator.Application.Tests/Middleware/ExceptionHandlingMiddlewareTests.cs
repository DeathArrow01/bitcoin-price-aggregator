using System;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using BitcoinPriceAggregator.Api.Middleware;
using BitcoinPriceAggregator.Application.Common.Models;
using System.Collections.Generic;
using BitcoinPriceAggregator.Domain.Exceptions;

namespace BitcoinPriceAggregator.Application.Tests.Middleware
{
    public class ExceptionHandlingMiddlewareTests
    {
        private readonly Mock<ILogger<ExceptionHandlingMiddleware>> _mockLogger;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };

        public ExceptionHandlingMiddlewareTests()
        {
            _mockLogger = new Mock<ILogger<ExceptionHandlingMiddleware>>();
        }

        [Fact]
        public async Task InvokeAsync_WhenValidationException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var errors = new[] { new FluentValidation.Results.ValidationFailure("Field", "Error message") };
            var next = new RequestDelegate(_ => throw new FluentValidation.ValidationException(errors));
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, JsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("Error message");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task InvokeAsync_WhenBitcoinPriceNotFoundException_ReturnsNotFound()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var timestamp = DateTime.UtcNow;
            var pair = "BTC/USD";
            var next = new RequestDelegate(_ => throw new BitcoinPriceNotFoundException(pair, timestamp));
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.NotFound);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, JsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be($"Bitcoin price not found for pair {pair} at timestamp {timestamp:yyyy-MM-dd HH:mm:ss}");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task InvokeAsync_WhenInvalidDateRangeException_ReturnsBadRequest()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var startTime = DateTime.UtcNow.AddHours(1);
            var endTime = DateTime.UtcNow;
            var next = new RequestDelegate(_ => throw new InvalidDateRangeException(startTime, endTime));
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, JsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be($"Invalid date range: start time {startTime:yyyy-MM-dd HH:mm:ss} is after end time {endTime:yyyy-MM-dd HH:mm:ss}");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task InvokeAsync_WhenExternalApiException_ReturnsBadGateway()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var provider = "TestProvider";
            var errorMessage = "API error";
            var next = new RequestDelegate(_ => throw new ExternalApiException(provider, errorMessage));
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadGateway);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, JsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be($"External API error from {provider}: Provider {provider}: {errorMessage}");
            result.Data.Should().BeNull();
        }

        [Fact]
        public async Task InvokeAsync_WhenUnhandledException_ReturnsInternalServerError()
        {
            // Arrange
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();
            var next = new RequestDelegate(_ => throw new Exception("Test exception"));
            var middleware = new ExceptionHandlingMiddleware(next, _mockLogger.Object);

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);
            context.Response.ContentType.Should().Be("application/json");

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var reader = new StreamReader(context.Response.Body);
            var responseBody = await reader.ReadToEndAsync();
            var result = JsonSerializer.Deserialize<ApiResponse<object>>(responseBody, JsonOptions);

            result.Should().NotBeNull();
            result!.Success.Should().BeFalse();
            result.Message.Should().Be("An unexpected error occurred");
            result.Data.Should().BeNull();
        }
    }
} 