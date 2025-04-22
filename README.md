# Pipelink.Outbox

A robust implementation of the outbox pattern for .NET applications using Entity Framework Core.

## Overview

Pipelink.Outbox is a .NET library that provides a reliable way to implement the outbox pattern in distributed systems. It ensures message delivery even in the face of system failures by storing messages in a database before attempting to publish them.

## Features

- ✅ Entity Framework Core integration
- ✅ Multiple message type support
- ✅ Automatic message publishing through background service
- ✅ Configurable retry policies
- ✅ Transactional message handling
- ✅ Comprehensive test coverage
- ✅ Support for batch processing
- ✅ Detailed error tracking and logging
- ✅ SQL Server, PostgreSQL, and SQLite support

## Getting Started

### Prerequisites

- .NET 6.0 or later
- Entity Framework Core
- A supported database (SQL Server, PostgreSQL, or SQLite)

### Installation

```bash
dotnet add package Pipelink.Outbox
```

### Basic Usage

1. Configure your DbContext:

```csharp
public class YourDbContext : DbContext
{
    public DbSet<OutboxMessage> OutboxMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfiguration(new OutboxMessageConfiguration());
    }
}
```

2. Use the outbox in your services:

```csharp
public class YourService
{
    private readonly YourDbContext _context;
    private readonly IOutboxPublisher _publisher;

    public YourService(YourDbContext context, IOutboxPublisher publisher)
    {
        _context = context;
        _publisher = publisher;
    }

    public async Task ProcessOrder(Order order)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        // Save your business data
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        // Add message to outbox
        await _publisher.PublishAsync(order);
        await transaction.CommitAsync();
    }
}
```

3. Configure the publisher and background service:

```csharp
services.AddOutboxPublisher(options =>
{
    options.RetryCount = 3;
    options.RetryInterval = TimeSpan.FromSeconds(5);
    options.BatchSize = 100;
});

// Add the background service
services.AddHostedService<OutboxPublisherBackgroundService>();
```

## Testing

The library includes comprehensive tests that cover:

- Message publishing and processing
- Error handling and retry mechanisms
- Batch processing
- Background service operation
- Database integration

### Running Tests

```bash
# Run all tests
dotnet test

# Run tests for a specific database
dotnet test --filter "Database=SqlServer"
dotnet test --filter "Database=Postgres"
dotnet test --filter "Database=Sqlite"
```

### Test Results

All tests are passing for:
- SQL Server
- PostgreSQL
- SQLite

## Message Processing

The background service automatically processes messages in the outbox. Messages go through the following states:

1. **Pending**: Initial state when a message is added to the outbox
2. **Processing**: When the message is being processed
3. **Completed**: When the message is successfully processed
4. **Failed**: When the message failed after all retry attempts

## Error Handling

The library provides robust error handling:
- Failed messages are automatically retried based on the configured retry count
- Each failure is logged with detailed error information
- Messages that exceed the retry count are marked as failed
- Error details are stored with the message for debugging

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Support

For support, please open an issue in the GitHub repository. 