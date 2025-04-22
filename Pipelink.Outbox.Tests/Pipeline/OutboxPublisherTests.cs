using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Pipeline;
using System;
using System.Threading.Tasks;
using Xunit;

namespace Pipelink.Outbox.Tests.Pipeline;

public class OutboxPublisherTests
{
    private readonly DbContextOptions<OutboxDbContext> _options;

    public OutboxPublisherTests()
    {
        _options = new DbContextOptionsBuilder<OutboxDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
    }

    [Fact]
    public async Task PublishAsync_ShouldCreateNewMessage()
    {
        // Arrange
        using var context = new OutboxDbContext(_options);
        var publisher = new OutboxPublisher(context);
        var testMessage = new TestMessage { Content = "Test content" };

        // Act
        await publisher.PublishAsync(testMessage);

        // Assert
        var message = await context.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(message);
        Assert.Equal(typeof(TestMessage).FullName, message.MessageType);
        Assert.Contains("Test content", message.Payload);
        Assert.Equal("Pending", message.Status);
        Assert.False(message.IsProcessed);
        Assert.Equal(0, message.RetryCount);
    }

    [Fact]
    public async Task PublishAsync_ShouldSetCorrectTimestamp()
    {
        // Arrange
        using var context = new OutboxDbContext(_options);
        var publisher = new OutboxPublisher(context);
        var testMessage = new TestMessage { Content = "Test content" };
        var beforeTest = DateTime.UtcNow;

        // Act
        await publisher.PublishAsync(testMessage);

        // Assert
        var message = await context.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(message);
        Assert.True(message.CreatedAt >= beforeTest);
        Assert.True(message.CreatedAt <= DateTime.UtcNow);
    }

    [Fact]
    public async Task PublishAsync_ShouldGenerateUniqueId()
    {
        // Arrange
        using var context = new OutboxDbContext(_options);
        var publisher = new OutboxPublisher(context);
        var testMessage1 = new TestMessage { Content = "Test 1" };
        var testMessage2 = new TestMessage { Content = "Test 2" };

        // Act
        await publisher.PublishAsync(testMessage1);
        await publisher.PublishAsync(testMessage2);

        // Assert
        var messages = await context.OutboxMessages.ToListAsync();
        Assert.Equal(2, messages.Count);
        Assert.NotEqual(messages[0].Id, messages[1].Id);
    }

    private class TestMessage
    {
        public string Content { get; set; } = string.Empty;
    }
} 