using System;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Steps;
using Xunit;

namespace Pipelink.Outbox.Tests.Steps
{
    public class SaveToOutboxStepTests
    {
        private readonly DbContextOptions<OutboxDbContext> _options;

        public SaveToOutboxStepTests()
        {
            _options = new DbContextOptionsBuilder<OutboxDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSaveMessageToDatabase()
        {
            // Arrange
            using var context = new OutboxDbContext(_options);
            var step = new SaveToOutboxStep(context);
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "TestMessage",
                Payload = "Test Payload",
                Status = "Pending"
            };

            // Act
            await step.ExecuteAsync(message);

            // Assert
            var savedMessage = await context.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
            Assert.NotNull(savedMessage);
            Assert.Equal("TestMessage", savedMessage.MessageType);
            Assert.Equal("Test Payload", savedMessage.Payload);
            Assert.Equal("Pending", savedMessage.Status);
            Assert.True(savedMessage.CreatedAt <= DateTime.UtcNow);
        }

        [Fact]
        public async Task ExecuteAsync_ShouldSetCorrectTimestamp()
        {
            // Arrange
            using var context = new OutboxDbContext(_options);
            var step = new SaveToOutboxStep(context);
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "TestMessage",
                Payload = "Test Payload",
                Status = "Pending"
            };

            // Act
            await step.ExecuteAsync(message);

            // Assert
            var savedMessage = await context.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
            Assert.NotNull(savedMessage);
            Assert.True(savedMessage.CreatedAt <= DateTime.UtcNow);
            Assert.True(savedMessage.CreatedAt >= DateTime.UtcNow.AddSeconds(-1));
        }
    }
} 