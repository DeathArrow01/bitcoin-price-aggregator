using System;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.Persistence;
using Moq;

namespace BitcoinPriceAggregator.IntegrationTests
{
    public class CustomWebApplicationFactory : WebApplicationFactory<Program>
    {
        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // Remove the existing DbContext registration
                services.RemoveAll(typeof(DbContextOptions<ApplicationDbContext>));

                // Add in-memory database
                services.AddDbContext<ApplicationDbContext>(options =>
                {
                    options.UseInMemoryDatabase("TestDb");
                });

                // Mock external API providers
                var mockBitstampProvider = new Mock<IPriceProvider>();
                mockBitstampProvider.Setup(x => x.ProviderName).Returns("Bitstamp");
                mockBitstampProvider.Setup(x => x.GetPriceAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
                    .ReturnsAsync(100.0m);

                var mockBitfinexProvider = new Mock<IPriceProvider>();
                mockBitfinexProvider.Setup(x => x.ProviderName).Returns("Bitfinex");
                mockBitfinexProvider.Setup(x => x.GetPriceAsync(It.IsAny<DateTime>(), It.IsAny<string>()))
                    .ReturnsAsync(200.0m);

                services.RemoveAll(typeof(IPriceProvider));
                services.AddScoped<IPriceProvider>(_ => mockBitstampProvider.Object);
                services.AddScoped<IPriceProvider>(_ => mockBitfinexProvider.Object);
            });
        }
    }
} 