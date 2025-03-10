using Microsoft.OpenApi.Models;
using System.Reflection;

namespace BitcoinPriceAggregator.Api.Configuration
{
    /// <summary>
    /// Configuration class for Swagger/OpenAPI documentation
    /// </summary>
    public static class SwaggerConfiguration
    {
        /// <summary>
        /// Configures Swagger/OpenAPI documentation services
        /// </summary>
        public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Bitcoin Price Aggregator API",
                    Version = "v1",
                    Description = "A service that aggregates Bitcoin prices from multiple sources.",
                    Contact = new OpenApiContact
                    {
                        Name = "API Support",
                        Email = "support@example.com"
                    }
                });

                // Configure JWT authentication in Swagger
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Example: \"Authorization: Bearer {token}\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.ApiKey,
                    Scheme = "Bearer"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Include XML comments from API project
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }

                // Include XML comments from Application layer
                var applicationXmlFile = "BitcoinPriceAggregator.Application.xml";
                var applicationXmlPath = Path.Combine(AppContext.BaseDirectory, applicationXmlFile);
                if (File.Exists(applicationXmlPath))
                {
                    c.IncludeXmlComments(applicationXmlPath);
                }
            });

            return services;
        }

        /// <summary>
        /// Configures Swagger/OpenAPI documentation middleware
        /// </summary>
        public static IApplicationBuilder UseSwaggerDocumentation(this IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            return app;
        }
    }
} 