using BitcoinPriceAggregator.Api.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace BitcoinPriceAggregator.Api.Configuration
{
    /// <summary>
    /// Configuration class for Authentication and Authorization
    /// </summary>
    public static class AuthenticationConfiguration
    {
        /// <summary>
        /// Configures JWT Authentication and Authorization
        /// </summary>
        public static IServiceCollection AddJwtAuthentication(this IServiceCollection services, IConfiguration configuration, IWebHostEnvironment environment)
        {
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer(options =>
                {
                    if (environment.IsDevelopment())
                    {
                        ConfigureDevelopmentJwt(options, configuration);
                    }
                    else
                    {
                        ConfigureProductionJwt(options, configuration);
                    }
                });

            services.AddAuthorization(options =>
            {
                ConfigureAuthorizationPolicies(options);
            });

            return services;
        }

        private static void ConfigureDevelopmentJwt(JwtBearerOptions options, IConfiguration configuration)
        {
            options.RequireHttpsMetadata = false;
            options.SaveToken = true;

            // Use the signing key from user secrets or environment variables
            var signingKeyValue = configuration["JwtSigningKey"] ?? "LxWXLF0q6VF8GsDNhBm9rOCzXpWr/kWjGqEIBhDmpLI=";

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(signingKeyValue)),
                ValidateIssuer = true,
                ValidIssuer = "dotnet-user-jwts",
                ValidateAudience = true,
                ValidAudiences = new[]
                {
                    "http://localhost:43716",
                    "https://localhost:44338",
                    "http://localhost:5041",
                    "https://localhost:7273"
                },
                ValidateLifetime = true,
                ClockSkew = TimeSpan.FromMinutes(5)
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    Console.WriteLine($"Authentication failed: {context.Exception}");
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    Console.WriteLine($"Token validated for user: {context.Principal?.Identity?.Name}");
                    return Task.CompletedTask;
                },
                OnMessageReceived = context =>
                {
                    Console.WriteLine($"Token received: {context.Token}");
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    Console.WriteLine($"Challenge issued: {context.Error}, {context.ErrorDescription}");
                    return Task.CompletedTask;
                }
            };
        }

        private static void ConfigureProductionJwt(JwtBearerOptions options, IConfiguration configuration)
        {
            var jwtSettings = configuration.GetSection("JwtSettings").Get<JwtSettings>();
            var key = Encoding.ASCII.GetBytes(jwtSettings?.SecretKey ??
                throw new InvalidOperationException("JWT Secret key is not configured"));

            options.RequireHttpsMetadata = true;
            options.SaveToken = true;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                ValidateLifetime = true,
                ClockSkew = TimeSpan.Zero
            };
        }

        private static void ConfigureAuthorizationPolicies(AuthorizationOptions options)
        {
            options.AddPolicy("ReadAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c =>
                        (c.Type == "scope" && c.Value == "api:read") ||
                        (c.Type == "scope" && c.Value == "api:full")
                    )
                ));

            options.AddPolicy("WriteAccess", policy =>
                policy.RequireAssertion(context =>
                    context.User.HasClaim(c => c.Type == "scope" && c.Value == "api:full")
                ));
        }
    }
} 