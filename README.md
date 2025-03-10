# Bitcoin Price Aggregator Service

A .NET microservice that aggregates Bitcoin (BTC/USD) prices from multiple sources, caches them, and provides a REST API for retrieving historical and current prices.

## Features

- Fetches Bitcoin prices from multiple sources (Bitstamp and Bitfinex)
- Aggregates prices using a configurable strategy (currently average)
- Caches results for improved performance
- Provides RESTful API endpoints
- Implements retry policies for external API calls
- Uses SQLite for persistent storage
- Includes comprehensive test suite

## Architecture

The service follows Clean Architecture principles and Domain-Driven Design:

- **Domain Layer**: Core business logic and entities
- **Application Layer**: Use cases and application logic
- **Infrastructure Layer**: External concerns (database, HTTP clients)
- **API Layer**: REST API endpoints and configuration

### Design Patterns

- CQRS (Command Query Responsibility Segregation)
- Repository Pattern
- Dependency Injection
- Factory Pattern
- Builder Pattern

## Prerequisites

- .NET 8.0 SDK
- SQLite

## Prepare access Token

1. Example token
Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJ1bmlxdWVfbmFtZSI6ImFkbWluIiwic3ViIjoiYWRtaW4iLCJqdGkiOiIxNjM2ODNjNiIsInJvbGUiOiJhcGktdXNlciIsInNjb3BlIjoiYXBpOmZ1bGwiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo3MjczLyIsIm5iZiI6MTc0MTUyOTM2OSwiZXhwIjoxNzQ5NDc4MTY5LCJpYXQiOjE3NDE1MjkzNjksImlzcyI6ImRvdG5ldC11c2VyLWp3dHMifQ.dz9i2DUeuxT4sCP0IF9q73cuWnJKHtR-h4b4zvSfXMQ

2. Add signing key to user secrets
   ```Powershell
   dotnet user-secrets set "JwtSigningKey" "LxWXLF0q6VF8GsDNhBm9rOCzXpWr/kWjGqEIBhDmpLI=" --project "src\BitcoinPriceAggregator.Api\BitcoinPriceAggregator.Api.csproj"
   ```

3. Generate JWT token to use in Swagger or Postman

   ```Powershell
   dotnet user-jwts create --name "admin" --role "api-user" --claim "scope=api:full" --audience "https://localhost:7273/" --project "src\BitcoinPriceAggregator.Api\BitcoinPriceAggregator.Api.csproj"
    ```


## Getting Started

1. Clone the repository:
   ```Powershell
   git clone https://github.com/DeathArrow01/bitcoin-price-aggregator.git
   ```

2. Navigate to the project directory:
   ```Powershell
   cd bitcoin-price-aggregator
   ```

3. Build the solution:
   ```Powershell
   dotnet build
   ```

4. Run the tests:
   ```Powershell
   dotnet test
   ```

5. Run the application:
   ```Powershell
   cd src/BitcoinPriceAggregator.Api
   dotnet run
   ```

## API Endpoints

### Get Price at Specific Time

```http
GET /api/v1/BitcoinPrice/{timestamp}?pair=BTC/USD
```

Parameters:
- `timestamp`: UTC timestamp (ISO 8601 format)
- `pair`: Trading pair (default: BTC/USD)

Response:
```json
{
  "data": {
    "pair": "BTC/USD",
    "price": 45000.50,
    "timestamp": "2024-03-08T12:00:00Z"
  }
}
```

### Get Price Range

```http
GET /api/v1/BitcoinPrice?startTime={startTime}&endTime={endTime}&pair=BTC/USD
```

Parameters:
- `startTime`: UTC start timestamp (ISO 8601 format)
- `endTime`: UTC end timestamp (ISO 8601 format)
- `pair`: Trading pair (default: BTC/USD)

Response:
```json
{
  "data": [
    {
      "pair": "BTC/USD",
      "price": 45000.50,
      "timestamp": "2024-03-08T12:00:00Z"
    },
    {
      "pair": "BTC/USD",
      "price": 45100.75,
      "timestamp": "2024-03-08T13:00:00Z"
    }
  ]
}
```

## Configuration

The application can be configured through `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=BitcoinPrices.db"
  },
  "CacheSettings": {
    "AbsoluteExpirationHours": 1
  },
  "RetryPolicySettings": {
    "MaxRetries": 3,
    "InitialRetrySeconds": 1
  }
}
```

## Testing

The solution includes:

1. Unit Tests:
   - Tests for query handlers
   - Tests for validators
   - Tests for domain logic

2. Integration Tests:
   - End-to-end API tests
   - Database integration tests
   - External API integration tests

Run tests with:
```Powershell
dotnet test
```

## Error Handling

The service implements comprehensive error handling:

- Validation errors return 400 Bad Request
- External API failures are retried with exponential backoff
- Unhandled exceptions return 500 Internal Server Error
- All errors are logged with Serilog

## Caching Strategy

The service implements a two-level caching strategy:

1. In-Memory Cache:
   - First level of caching
   - Configurable expiration time
   - Thread-safe implementation

2. SQLite Database:
   - Second level of caching
   - Persistent storage
   - Hour-level precision