# Pipelink.Outbox.Tests

A sample Web API project demonstrating the usage of the Pipelink.Outbox library with a working implementation of the Outbox pattern.

## Features

- RESTful API endpoints for message management
- In-memory database for easy testing
- Swagger UI for API documentation and testing
- Example pipeline implementation
- Comprehensive test suite including unit and integration tests

## Getting Started

### Prerequisites

- .NET 8.0 SDK or later
- Your favorite IDE (Visual Studio, VS Code, Rider)

### Running the Project

1. Clone the repository
2. Navigate to the project directory:
```bash
cd Pipelink.Outbox.Tests
```
3. Run the project:
```bash
dotnet run
```
4. Open your browser and navigate to:
   - Swagger UI: https://localhost:5001/swagger
   - API Base URL: https://localhost:5001/api

## API Endpoints

### Create Message
```http
POST /api/outbox
Content-Type: application/json

{
    "messageType": "string",
    "payload": "string"
}
```

### Get All Messages
```http
GET /api/outbox
```

## Testing

### Unit Tests

The project includes unit tests for:
- `SaveToOutboxStep`: Tests message persistence
- `OutboxPipeline`: Tests pipeline execution and ordering

To run unit tests:
```bash
dotnet test --filter "Category=Unit"
```

### Integration Tests

Integration tests cover:
- API endpoint functionality
- Database integration
- Complete request-response cycle

To run integration tests:
```bash
dotnet test --filter "Category=Integration"
```

### Test Coverage

The project uses Coverlet for test coverage reporting. To generate coverage reports:
```bash
dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=opencover
```

## Testing Different Databases

The project uses an in-memory database by default. To test with a real database:

1. Add the appropriate NuGet package:
```bash
# For SQL Server
dotnet add package Microsoft.EntityFrameworkCore.SqlServer

# For PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL

# For MySQL
dotnet add package Pomelo.EntityFrameworkCore.MySql
```

2. Update the connection string in `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Your-Connection-String-Here"
  }
}
```

3. Modify the DbContext registration in `Program.cs` to use your chosen provider.

## Contributing

Feel free to submit issues and enhancement requests!

## Test Structure

```
Pipelink.Outbox.Tests/
├── Integration/
│   └── OutboxIntegrationTests.cs    # API and database integration tests
├── Pipeline/
│   └── OutboxPipelineTests.cs       # Pipeline execution tests
├── Steps/
│   └── SaveToOutboxStepTests.cs     # Step-specific tests
└── Program.cs                       # Test application entry point
``` 