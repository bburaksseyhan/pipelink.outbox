# Pipelink.Outbox

A .NET library for implementing the outbox pattern with Entity Framework Core.

## Overview

Pipelink.Outbox provides a simple and efficient way to implement the outbox pattern in your .NET applications using Entity Framework Core. The outbox pattern is a reliable way to handle message publishing in distributed systems, ensuring that messages are not lost even if the system fails.

## Features

- Easy integration with Entity Framework Core
- Support for multiple message types
- Automatic message publishing
- Configurable retry policies
- Transactional message handling

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
        var message = new OutboxMessage
        {
            MessageType = "OrderCreated",
            Payload = JsonSerializer.Serialize(order),
            CreatedAt = DateTime.UtcNow
        };
        _context.OutboxMessages.Add(message);
        await _context.SaveChangesAsync();

        await transaction.CommitAsync();
    }
}
```

3. Configure the publisher in your startup:

```csharp
services.AddOutboxPublisher(options =>
{
    options.RetryCount = 3;
    options.RetryInterval = TimeSpan.FromSeconds(5);
});
```

## Configuration Options

- `RetryCount`: Number of retry attempts for failed messages (default: 3)
- `RetryInterval`: Time between retry attempts (default: 5 seconds)
- `BatchSize`: Number of messages to process in a single batch (default: 100)

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

## Support

If you encounter any issues or have questions, please open an issue on GitHub. 