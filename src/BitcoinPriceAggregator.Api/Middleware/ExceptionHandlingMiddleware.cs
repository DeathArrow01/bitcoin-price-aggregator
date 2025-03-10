using BitcoinPriceAggregator.Application.Common.Models;
using BitcoinPriceAggregator.Domain.Exceptions;
using FluentValidation;
using System.Net;
using System.Text.Json;

namespace BitcoinPriceAggregator.Api.Middleware
{
    /// <summary>
    /// Middleware for handling exceptions and converting them to appropriate HTTP responses
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        };

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An unhandled exception occurred");
                await HandleExceptionAsync(context, ex);
            }
        }

        private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
        {
            var response = context.Response;
            response.ContentType = "application/json";

            var (statusCode, message) = exception switch
            {
                BitcoinPriceNotFoundException ex => (HttpStatusCode.NotFound, ex.Message),
                InvalidDateRangeException ex => (HttpStatusCode.BadRequest, ex.Message),
                ExternalApiException ex => (HttpStatusCode.BadGateway, $"External API error from {ex.Provider}: {ex.Message}"),
                FluentValidation.ValidationException ex => (HttpStatusCode.BadRequest, string.Join(", ", ex.Errors.Select(e => e.ErrorMessage))),
                _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
            };

            response.StatusCode = (int)statusCode;

            var apiResponse = new ApiResponse<object>
            {
                Success = false,
                Message = message,
                Data = null
            };

            var result = JsonSerializer.Serialize(apiResponse, JsonOptions);
            await response.WriteAsync(result);
        }
    }
}