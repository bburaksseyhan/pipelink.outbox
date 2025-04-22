# Pipelink.Outbox

A .NET library for implementing the outbox pattern with Entity Framework Core.

## Features

- Easy integration with Entity Framework Core
- Reliable message delivery
- Transactional outbox pattern implementation
- Support for .NET 8.0

## Installation

```bash
dotnet add package Pipelink.Outbox
```

## Usage

```csharp
// Add the outbox service to your DI container
services.AddOutbox<YourDbContext>();

// Use the outbox in your services
public class YourService
{
    private readonly IOutbox _outbox;

    public YourService(IOutbox outbox)
    {
        _outbox = outbox;
    }

    public async Task ProcessMessage()
    {
        await _outbox.PublishAsync(new YourMessage());
    }
}
```

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request. 