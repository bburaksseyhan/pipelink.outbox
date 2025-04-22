# Pipelink.Outbox

A .NET library for implementing the outbox pattern with Entity Framework Core.

## Overview

Pipelink.Outbox provides a simple and efficient way to implement the outbox pattern in your .NET applications using Entity Framework Core. The outbox pattern is a reliable way to handle message publishing in distributed systems, ensuring that messages are not lost even if the system fails.

## Features

- Easy integration with Entity Framework Core
- Support for multiple message types
- Automatic message publishing through background service
- Configurable retry policies
- Transactional message handling
- Comprehensive test coverage
- Support for batch processing
- Detailed error tracking and logging

## Installation

```bash
dotnet add package Pipelink.Outbox
```

## Quick Start

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

3. Configure the publisher and background service in your startup:

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

## Configuration Options

- `RetryCount`: Number of retry attempts for failed messages (default: 3)
- `RetryInterval`: Time between retry attempts (default: 5 seconds)
- `BatchSize`: Number of messages to process in a single batch (default: 100)

## Message Processing

The library includes a background service that automatically processes messages in the outbox. Messages go through the following states:

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

## Testing

The library includes comprehensive tests covering:
- Message publishing
- Message processing
- Error handling
- Retry mechanisms
- Batch processing
- Background service operation

To run the tests:
```bash
dotnet test
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

For support, please open an issue in the GitHub repository. 