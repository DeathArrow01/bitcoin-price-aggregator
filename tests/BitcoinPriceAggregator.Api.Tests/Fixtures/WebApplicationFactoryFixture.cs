using BitcoinPriceAggregator.Api.Controllers;
using BitcoinPriceAggregator.Application.BackgroundServices;
using BitcoinPriceAggregator.Application.DTOs;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Application.Queries.Validators;
using BitcoinPriceAggregator.Domain.Entities;
using BitcoinPriceAggregator.Domain.Repositories;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.Persistence;
using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Moq;
using System.Security.Claims;
using System.Text.Encodings.Web;

namespace BitcoinPriceAggregator.Api.Tests.Fixtures
{
    public class WebApplicationFactoryFixture : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureAppConfiguration((context, config) =>
            {
                var configuration = new Dictionary<string, string?>
                {
                    ["CacheSettings:ExpirationTimeInMinutes"] = "60",
                    ["RetryPolicySettings:TotalRetries"] = "3",
                    ["RetryPolicySettings:ImmediateRetries"] = "1",
                    ["RetryPolicySettings:BaseDelayInSeconds"] = "2",
                    ["BackgroundServices:CachePriming:IntervalMinutes"] = "0",
                    ["BackgroundServices:DatabaseMaintenance:IntervalHours"] = "0"
                };
                config.AddInMemoryCollection(configuration);
            });

            builder.ConfigureTestServices(services =>
            {
                // Remove all hosted services and background services
                services.RemoveAll(typeof(IHostedService));
                services.RemoveAll(typeof(CachePrimingService));
                services.RemoveAll(typeof(DatabaseMaintenanceService));

                // Remove any database-related services
                services.RemoveAll(typeof(ApplicationDbContext));
                services.RemoveAll(typeof(IBitcoinPriceRepository));

                // Mock authorization
                var mockAuthorizationService = new Mock<IAuthorizationService>();
                mockAuthorizationService
                    .Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<string>()))
                    .ReturnsAsync(AuthorizationResult.Success());
                services.AddSingleton(mockAuthorizationService.Object);

                // Mock mediator
                var mockMediator = new Mock<IMediator>();
                mockMediator.Setup(m => m.Send(It.IsAny<GetAggregatedPriceQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((GetAggregatedPriceQuery query, CancellationToken _) =>
                    {
                        var validator = new GetAggregatedPriceQueryValidator();
                        var validationResult = validator.Validate(query);
                        if (!validationResult.IsValid)
                        {
                            throw new FluentValidation.ValidationException(validationResult.Errors);
                        }

                        return new BitcoinPriceDto
                        {
                            Pair = query.Pair,
                            Price = 42000.00m,
                            Timestamp = new DateTimeOffset(query.UtcTicks, TimeSpan.Zero)
                        };
                    });

                mockMediator.Setup(m => m.Send(It.IsAny<GetPriceRangeQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((GetPriceRangeQuery query, CancellationToken _) =>
                    {
                        var validator = new GetPriceRangeQueryValidator();
                        var validationResult = validator.Validate(query);
                        if (!validationResult.IsValid)
                        {
                            throw new FluentValidation.ValidationException(validationResult.Errors);
                        }

                        return new List<BitcoinPriceDto>
                        {
                            new BitcoinPriceDto
                            {
                                Pair = query.Pair,
                                Price = 42000.00m,
                                Timestamp = new DateTimeOffset(query.StartTicks, TimeSpan.Zero)
                            },
                            new BitcoinPriceDto
                            {
                                Pair = query.Pair,
                                Price = 43000.00m,
                                Timestamp = new DateTimeOffset(query.EndTicks, TimeSpan.Zero)
                            }
                        };
                    });

                // Mock repository
                var mockRepository = new Mock<IBitcoinPriceRepository>();
                mockRepository.Setup(r => r.GetPriceAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((long ticks, string pair, CancellationToken _) =>
                        BitcoinPrice.CreateBuilder()
                            .WithPair(pair)
                            .WithPrice(42000.00m)
                            .WithUtcTicks(ticks)
                            .Build());

                mockRepository.Setup(r => r.GetPriceRangeAsync(It.IsAny<long>(), It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync((long startTicks, long endTicks, string pair, CancellationToken _) =>
                        new List<BitcoinPrice>
                        {
                            BitcoinPrice.CreateBuilder()
                                .WithPair(pair)
                                .WithPrice(42000.00m)
                                .WithUtcTicks(startTicks)
                                .Build(),
                            BitcoinPrice.CreateBuilder()
                                .WithPair(pair)
                                .WithPrice(43000.00m)
                                .WithUtcTicks(endTicks)
                                .Build()
                        });

                // Mock price providers
                var mockBitstampProvider = new Mock<IPriceProvider>();
                mockBitstampProvider.Setup(p => p.ProviderName).Returns("Bitstamp");
                mockBitstampProvider.Setup(p => p.GetPriceAsync(It.IsAny<long>(), It.IsAny<string>()))
                    .ReturnsAsync(42000.00m);

                var mockBitfinexProvider = new Mock<IPriceProvider>();
                mockBitfinexProvider.Setup(p => p.ProviderName).Returns("Bitfinex");
                mockBitfinexProvider.Setup(p => p.GetPriceAsync(It.IsAny<long>(), It.IsAny<string>()))
                    .ReturnsAsync(42100.00m);

                // Mock logger
                var mockLogger = new Mock<ILogger<BitcoinPriceController>>();

                // Configure time provider
                var timeProvider = new FakeTimeProvider(new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero));

                // Register mocks
                services.AddScoped<IMediator>(sp => mockMediator.Object);
                services.AddScoped<IBitcoinPriceRepository>(sp => mockRepository.Object);
                services.AddScoped<IPriceProvider>(sp => mockBitstampProvider.Object);
                services.AddScoped<IPriceProvider>(sp => mockBitfinexProvider.Object);
                services.AddScoped(sp => mockLogger.Object);
                services.AddSingleton<TimeProvider>(timeProvider);

                // Configure test authentication
                services.AddAuthentication(defaultScheme: "TestScheme")
                    .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                        "TestScheme", options => { });

                // Add validators and configure FluentValidation
                services.AddControllers();
                services.AddValidatorsFromAssemblyContaining<GetAggregatedPriceQueryValidator>();
                services.AddValidatorsFromAssemblyContaining<GetPriceRangeQueryValidator>();

                // Configure MediatR
                services.AddMediatR(cfg =>
                {
                    cfg.RegisterServicesFromAssembly(typeof(GetAggregatedPriceQuery).Assembly);
                });

                // Add logging
                services.AddLogging(builder =>
                {
                    builder.AddConsole();
                    builder.SetMinimumLevel(LogLevel.Debug);
                });
            });
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
            var claims = new[]
            {
                new Claim(ClaimTypes.Name, "Test User"),
                new Claim(ClaimTypes.Role, "User"),
                new Claim("ReadAccess", "true")
            };

            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, "TestScheme");

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}