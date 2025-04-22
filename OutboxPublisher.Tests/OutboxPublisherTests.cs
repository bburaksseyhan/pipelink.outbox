using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Pipeline;
using System;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace OutboxPublisher.Tests;

public class TestMessage
{
    public string Content { get; set; }
}

public class OutboxPublisherTests : IDisposable
{
    private readonly Mock<IOutboxPipeline<OutboxMessage>> _pipelineMock;
    private readonly OutboxDbContext _dbContext;
    private readonly Pipelink.Outbox.Pipeline.OutboxPublisher _publisher;
    private readonly ServiceProvider _serviceProvider;

    public OutboxPublisherTests()
    {
        _pipelineMock = new Mock<IOutboxPipeline<OutboxMessage>>();

        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString())); // Use unique database for each test

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<OutboxDbContext>();

        _publisher = new Pipelink.Outbox.Pipeline.OutboxPublisher(_dbContext, _pipelineMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _serviceProvider.Dispose();
    }

    [Fact]
    public async Task PublishAsync_ShouldCreateOutboxMessage()
    {
        // Arrange
        var message = new TestMessage { Content = "Test content" };

        // Act
        await _publisher.PublishAsync(message);

        // Assert
        var savedMessage = await _dbContext.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(savedMessage);
        Assert.Equal(typeof(TestMessage).FullName, savedMessage.MessageType);
        Assert.Equal(JsonSerializer.Serialize(message), savedMessage.Payload);
        Assert.Equal("Pending", savedMessage.Status);
        Assert.False(savedMessage.IsProcessed);
        Assert.Equal(0, savedMessage.RetryCount);
        Assert.Null(savedMessage.ProcessedAt);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldProcessMessages()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestMessage).FullName,
            Payload = JsonSerializer.Serialize(new TestMessage { Content = "Test content" }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _publisher.ProcessPendingMessagesAsync();

        // Assert
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Once);
        var processedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.True(processedMessage.IsProcessed);
        Assert.NotNull(processedMessage.ProcessedAt);
        Assert.Equal("Completed", processedMessage.Status);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldHandleFailedMessages()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestMessage).FullName,
            Payload = JsonSerializer.Serialize(new TestMessage { Content = "Test content" }),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        _pipelineMock.Setup(p => p.ProcessAsync(It.IsAny<OutboxMessage>()))
            .ThrowsAsync(new Exception("Test failure"));

        // Act
        await _publisher.ProcessPendingMessagesAsync();

        // Assert
        var failedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.False(failedMessage.IsProcessed);
        Assert.Equal(1, failedMessage.RetryCount);
        Assert.Equal("Test failure", failedMessage.LastError);
        Assert.NotNull(failedMessage.LastErrorAt);
        Assert.Equal("Pending", failedMessage.Status);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldNotProcessAlreadyProcessedMessages()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestMessage).FullName,
            Payload = JsonSerializer.Serialize(new TestMessage { Content = "Test content" }),
            Status = "Completed",
            IsProcessed = true,
            ProcessedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await _publisher.ProcessPendingMessagesAsync();

        // Assert
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPendingMessagesAsync_ShouldMarkMessageAsFailedAfterMaxRetries()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(TestMessage).FullName,
            Payload = JsonSerializer.Serialize(new TestMessage { Content = "Test content" }),
            Status = "Pending",
            RetryCount = 2, // One retry remaining
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        _pipelineMock.Setup(p => p.ProcessAsync(It.IsAny<OutboxMessage>()))
            .ThrowsAsync(new Exception("Test failure"));

        // Act
        await _publisher.ProcessPendingMessagesAsync();

        // Assert
        var failedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.False(failedMessage.IsProcessed);
        Assert.Equal(3, failedMessage.RetryCount);
        Assert.Equal("Test failure", failedMessage.LastError);
        Assert.NotNull(failedMessage.LastErrorAt);
        Assert.Equal("Failed", failedMessage.Status);
    }
} 