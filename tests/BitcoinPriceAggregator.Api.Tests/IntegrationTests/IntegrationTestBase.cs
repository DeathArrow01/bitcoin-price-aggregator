using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using System.Net.Http.Headers;
using Xunit;

namespace BitcoinPriceAggregator.Api.Tests.IntegrationTests
{
    public class WebApplicationFactoryFixture : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var settings = new List<KeyValuePair<string, string?>>
                {
                    new("ConnectionStrings:DefaultConnection", "DataSource=:memory:;Cache=Shared"),
                    new("RetryPolicySettings:TotalRetries", "3"),
                    new("RetryPolicySettings:ImmediateRetries", "1"),
                    new("RetryPolicySettings:BaseDelayInSeconds", "2"),
                    new("CacheSettings:DefaultExpirationMinutes", "60"),
                    new("CacheSettings:MaxCacheItems", "1000"),
                    new("ExternalApiSettings:BitstampApiUrl", "https://www.bitstamp.net/api/v2/ticker/btcusd/"),
                    new("ExternalApiSettings:BitfinexApiUrl", "https://api.bitfinex.com/v1/pubticker/btcusd")
                };

                config.AddInMemoryCollection(settings);
            });

            builder.ConfigureTestServices(services =>
            {
                // Remove existing authentication and authorization
                var descriptors = services.Where(d => 
                    d.ServiceType == typeof(IAuthenticationService) ||
                    d.ServiceType == typeof(IAuthorizationHandler) ||
                    d.ServiceType == typeof(IAuthorizationService) ||
                    d.ServiceType == typeof(AuthenticationOptions)).ToList();

                foreach (var descriptor in descriptors)
                {
                    services.Remove(descriptor);
                }

                // Add test authentication
                services.AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = "Test";
                    options.DefaultChallengeScheme = "Test";
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>("Test", options => { });

                // Configure test authorization
                services.AddAuthorization(options =>
                {
                    options.AddPolicy("ReadAccess", policy => policy.RequireAuthenticatedUser());
                });

                // Configure background services
                services.Configure<HostOptions>(options =>
                {
                    options.BackgroundServiceExceptionBehavior = BackgroundServiceExceptionBehavior.Ignore;
                });
            });

            builder.UseEnvironment("Development");
        }
    }

    public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
    {
        public TestAuthHandler(
            IOptionsMonitor<AuthenticationSchemeOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder)
            : base(options, logger, encoder)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var claims = new[] { new Claim(ClaimTypes.Name, "Test User") };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "Test");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }

    [CollectionDefinition("Integration Tests")]
    public class IntegrationTestCollection : ICollectionFixture<WebApplicationFactoryFixture>
    {
    }

    [Collection("Integration Tests")]
    public abstract class IntegrationTestBase : IAsyncDisposable
    {
        protected readonly WebApplicationFactoryFixture Factory;
        protected readonly WebApplicationFactory<Program> _factory;
        protected readonly HttpClient _client;

        protected IntegrationTestBase(WebApplicationFactoryFixture factory)
        {
            Factory = factory;

            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        var settings = new Dictionary<string, string?>
                        {
                            ["JwtSettings:Secret"] = "your-256-bit-secret",
                            ["JwtSettings:Issuer"] = "your-issuer",
                            ["JwtSettings:Audience"] = "your-audience",
                            ["JwtSettings:ExpiryInMinutes"] = "60"
                        };
                        config.AddInMemoryCollection(settings);
                    });

                    builder.ConfigureServices(services =>
                    {
                        services.Configure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
                        {
                            options.RequireHttpsMetadata = false;
                            options.SaveToken = true;
                        });
                    });
                });

            _client = _factory.CreateClient();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public async ValueTask DisposeAsync()
        {
            await _factory.DisposeAsync();
            _client.Dispose();
        }
    }
} 