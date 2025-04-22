using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Pipeline;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OutboxPublisher.Tests.BackgroundService;

public class OutboxPublisherBackgroundServiceTests : IDisposable
{
    private readonly Mock<ILogger<OutboxPublisherBackgroundService>> _loggerMock;
    private readonly Mock<IOutboxPipeline<OutboxMessage>> _pipelineMock;
    private readonly OutboxDbContext _dbContext;
    private readonly ServiceProvider _serviceProvider;
    private readonly OutboxPublisherOptions _options;
    private readonly OutboxPublisherBackgroundService _service;

    public OutboxPublisherBackgroundServiceTests()
    {
        _loggerMock = new Mock<ILogger<OutboxPublisherBackgroundService>>();
        _pipelineMock = new Mock<IOutboxPipeline<OutboxMessage>>();

        var services = new ServiceCollection();
        services.AddDbContext<OutboxDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        _serviceProvider = services.BuildServiceProvider();
        _dbContext = _serviceProvider.GetRequiredService<OutboxDbContext>();

        _options = new OutboxPublisherOptions
        {
            RetryCount = 3,
            RetryInterval = TimeSpan.FromMilliseconds(500), // Increased to reduce multiple processing cycles
            BatchSize = 10
        };

        var serviceProviderMock = new Mock<IServiceProvider>();
        serviceProviderMock
            .Setup(x => x.GetService(typeof(OutboxDbContext)))
            .Returns(_dbContext);
        serviceProviderMock
            .Setup(x => x.GetService(typeof(IOutboxPipeline<OutboxMessage>)))
            .Returns(_pipelineMock.Object);

        var scopeFactory = new Mock<IServiceScope>();
        scopeFactory.Setup(x => x.ServiceProvider).Returns(serviceProviderMock.Object);

        var scopeFactoryProvider = new Mock<IServiceScopeFactory>();
        scopeFactoryProvider
            .Setup(x => x.CreateScope())
            .Returns(scopeFactory.Object);

        serviceProviderMock
            .Setup(x => x.GetService(typeof(IServiceScopeFactory)))
            .Returns(scopeFactoryProvider.Object);

        _service = new OutboxPublisherBackgroundService(
            serviceProviderMock.Object,
            _options,
            _loggerMock.Object);
    }

    public void Dispose()
    {
        _dbContext.Database.EnsureDeleted();
        _serviceProvider.Dispose();
    }

    private async Task RunBackgroundServiceOnce()
    {
        using var cts = new CancellationTokenSource();
        var serviceTask = _service.StartAsync(cts.Token);
        
        // Wait for half the retry interval to ensure one processing cycle
        await Task.Delay(_options.RetryInterval / 2);
        
        await _service.StopAsync(cts.Token);
        await serviceTask;
    }

    [Fact]
    public async Task StartAsync_ShouldProcessPendingMessages()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = "Test payload",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await RunBackgroundServiceOnce();

        // Assert
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Once);
        var processedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(processedMessage);
        Assert.True(processedMessage.IsProcessed);
        Assert.Equal("Completed", processedMessage.Status);
    }

    [Fact]
    public async Task StartAsync_ShouldHandleFailedMessages()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = "Test payload",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        _pipelineMock.Setup(p => p.ProcessAsync(It.IsAny<OutboxMessage>()))
            .ThrowsAsync(new Exception("Test failure"));

        // Act
        await RunBackgroundServiceOnce();

        // Assert
        var failedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(failedMessage);
        Assert.False(failedMessage.IsProcessed);
        Assert.Equal(1, failedMessage.RetryCount);
        Assert.Equal("Test failure", failedMessage.LastError);
        Assert.NotNull(failedMessage.LastErrorAt);
        Assert.Equal("Pending", failedMessage.Status);
    }

    [Fact]
    public async Task StartAsync_ShouldRespectBatchSize()
    {
        // Arrange
        for (int i = 0; i < 15; i++) // Add more messages than batch size
        {
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "TestMessage",
                Payload = $"Test payload {i}",
                Status = "Pending",
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.OutboxMessages.AddAsync(message);
        }
        await _dbContext.SaveChangesAsync();

        // Act
        await RunBackgroundServiceOnce();

        // Assert
        var processedMessages = await _dbContext.OutboxMessages
            .Where(m => m.IsProcessed)
            .CountAsync();
        Assert.Equal(_options.BatchSize, processedMessages);
        _pipelineMock.Verify(p => p.ProcessAsync(It.IsAny<OutboxMessage>()), Times.Exactly(_options.BatchSize));
    }

    [Fact]
    public async Task StartAsync_ShouldRespectRetryCount()
    {
        // Arrange
        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = "Test payload",
            Status = "Pending",
            RetryCount = _options.RetryCount - 1, // One retry remaining
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        _pipelineMock.Setup(p => p.ProcessAsync(It.IsAny<OutboxMessage>()))
            .ThrowsAsync(new Exception("Test failure"));

        // Act
        await RunBackgroundServiceOnce();

        // Assert
        var failedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(failedMessage);
        Assert.False(failedMessage.IsProcessed);
        Assert.Equal(_options.RetryCount, failedMessage.RetryCount);
        Assert.Equal("Failed", failedMessage.Status);
    }

    [Fact]
    public async Task StartAsync_ShouldHandleExceptions()
    {
        // Arrange
        _pipelineMock.Setup(p => p.ProcessAsync(It.IsAny<OutboxMessage>()))
            .ThrowsAsync(new Exception("Unexpected error"));

        var message = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = "TestMessage",
            Payload = "Test payload",
            Status = "Pending",
            CreatedAt = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(message);
        await _dbContext.SaveChangesAsync();

        // Act
        await RunBackgroundServiceOnce();

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => true),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);

        var updatedMessage = await _dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updatedMessage);
        Assert.Equal(1, updatedMessage.RetryCount);
        Assert.Equal("Unexpected error", updatedMessage.LastError);
    }
} 