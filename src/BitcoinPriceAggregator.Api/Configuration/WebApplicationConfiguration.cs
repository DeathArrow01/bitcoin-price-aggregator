using BitcoinPriceAggregator.Api.Middleware;
using BitcoinPriceAggregator.Infrastructure.Persistence;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using FluentValidation.AspNetCore;
using BitcoinPriceAggregator.Application.Queries.Validators;

namespace BitcoinPriceAggregator.Api.Configuration
{
    /// <summary>
    /// Configuration class for web application setup and middleware
    /// </summary>
    public static class WebApplicationConfiguration
    {
        /// <summary>
        /// Configures controllers and JSON options
        /// </summary>
        public static IServiceCollection AddWebApiConfiguration(this IServiceCollection services)
        {
            services.AddControllers()
                .AddJsonOptions(options =>
                {
                    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                    options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
                    options.JsonSerializerOptions.WriteIndented = true;
                });

            services.AddFluentValidationAutoValidation()
                   .AddFluentValidationClientsideAdapters()
                   .AddValidatorsFromAssemblyContaining<GetAggregatedPriceQueryValidator>()
                   .AddValidatorsFromAssemblyContaining<GetPriceRangeQueryValidator>();

            return services;
        }

        /// <summary>
        /// Configures the HTTP request pipeline and middleware
        /// </summary>
        public static WebApplication UseWebApiConfiguration(this WebApplication app)
        {
            app.UseMiddleware<ExceptionHandlingMiddleware>();
            app.UseHttpsRedirection();

            app.UseAuthentication();
            app.UseAuthorization();

            app.MapHealthChecks("/healthcheck");
            app.MapControllers();

            return app;
        }

        /// <summary>
        /// Ensures the database is created
        /// </summary>
        public static WebApplication EnsureDatabaseCreated(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                context.Database.EnsureCreated();
            }

            return app;
        }
    }
} 