using BitcoinPriceAggregator.Api.Configuration;
using BitcoinPriceAggregator.Api.Middleware;
using BitcoinPriceAggregator.Application.Mapping;
using BitcoinPriceAggregator.Application.Queries;
using BitcoinPriceAggregator.Application.Queries.Validators;
using FluentValidation;
using Serilog;
using BitcoinPriceAggregator.Domain.Services;
using BitcoinPriceAggregator.Infrastructure.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
builder.Host.UseSerilog((context, config) => config.ReadFrom.Configuration(context.Configuration));

// Configure web API
builder.Services.AddWebApiConfiguration();

// Add Swagger documentation
builder.Services.AddSwaggerDocumentation();

// Add JWT Authentication and Authorization
builder.Services.AddJwtAuthentication(builder.Configuration, builder.Environment);

// Configure MediatR
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(GetAggregatedPriceQuery).Assembly);
});

// Configure AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// Configure Validators
builder.Services.AddValidatorsFromAssemblyContaining<GetAggregatedPriceQueryValidator>();

// Add Application Services and Settings
builder.Services.AddApplicationServices(builder.Configuration);

// Register services
builder.Services.AddScoped<IPriceAggregatorService, PriceAggregatorService>();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();
app.UseAuthorization();
app.UseWebApiConfiguration();
app.MapControllers();

try
{
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
    throw;
}
finally
{
    Log.CloseAndFlush();
}

// Make the Program class public for testing
public partial class Program { }
