using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Moq;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Pipeline;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Pipelink.Outbox.Tests.Pipeline;

public class OutboxPublisherBackgroundServiceTests
{
    private readonly Mock<ILogger<OutboxPublisherBackgroundService>> _loggerMock;
    private readonly Mock<IOutboxPipeline<OutboxMessage>> _pipelineMock;
    private readonly OutboxPublisherOptions _options;
    private readonly ServiceProvider _serviceProvider;

    public OutboxPublisherBackgroundServiceTests()
    {
        _loggerMock = new Mock<ILogger<OutboxPublisherBackgroundService>>();
        _pipelineMock = new Mock<IOutboxPipeline<OutboxMessage>>();
        _options = new OutboxPublisherOptions
        {
            RetryCount = 3,
            RetryInterval = TimeSpan.FromMilliseconds(100),
            BatchSize = 10
        };

        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase("TestDb"));
        services.AddSingleton(_pipelineMock.Object);
        services.AddSingleton(_options);
        services.AddSingleton(_loggerMock.Object);
        services.AddSingleton<OutboxPublisherBackgroundService>();

        _serviceProvider = services.BuildServiceProvider();
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldProcessMessagesSuccessfully()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<OutboxDbContext>();
        var service = _serviceProvider.GetRequiredService<OutboxPublisherBackgroundService>();

        // Add test messages
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = "Test payload",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        // Act
        var processTask = service.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Wait for processing
        await service.StopAsync(CancellationToken.None);
        await processTask;

        // Assert
        var processedMessage = await dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(processedMessage);
        Assert.True(processedMessage.IsProcessed);
        Assert.Equal("Completed", processedMessage.Status);
        Assert.NotNull(processedMessage.ProcessedAt);
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldHandleFailedMessages()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<OutboxDbContext>();
        var service = _serviceProvider.GetRequiredService<OutboxPublisherBackgroundService>();

        // Add test message
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = "Test payload",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        // Setup pipeline to throw exception
        _pipelineMock.Setup(p => p.ProcessAsync(It.IsAny<OutboxMessage>()))
            .ThrowsAsync(new Exception("Test failure"));

        // Act
        var processTask = service.StartAsync(CancellationToken.None);
        await Task.Delay(200); // Wait for processing
        await service.StopAsync(CancellationToken.None);
        await processTask;

        // Assert
        var failedMessage = await dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(failedMessage);
        Assert.False(failedMessage.IsProcessed);
        Assert.Equal(1, failedMessage.RetryCount);
        Assert.Equal("Test failure", failedMessage.LastError);
        Assert.NotNull(failedMessage.LastErrorAt);
        Assert.Equal("Pending", failedMessage.Status);
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPendingMessages_ShouldProcessMessagesInBatches()
    {
        // Arrange
        var dbContext = _serviceProvider.GetRequiredService<OutboxDbContext>();
        var service = _serviceProvider.GetRequiredService<OutboxPublisherBackgroundService>();

        // Add more messages than batch size
        for (int i = 0; i < 15; i++)
        {
            dbContext.OutboxMessages.Add(new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "TestMessage",
                Payload = $"Test payload {i}",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            });
        }
        await dbContext.SaveChangesAsync();

        // Act
        var processTask = service.StartAsync(CancellationToken.None);
        await Task.Delay(50); // Wait for a short time to process one batch
        await service.StopAsync(CancellationToken.None);
        await processTask;

        // Assert
        var processedMessages = await dbContext.OutboxMessages
            .Where(m => m.IsProcessed)
            .ToListAsync();
        Assert.Equal(10, processedMessages.Count); // Should process only batch size
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Exactly(10));
    }
} 