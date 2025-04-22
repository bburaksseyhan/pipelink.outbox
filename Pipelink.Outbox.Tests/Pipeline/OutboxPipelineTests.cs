using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using Pipelink.Outbox.Pipeline;
using Pipelink.Outbox.Steps;
using Xunit;

namespace Pipelink.Outbox.Tests.Pipeline
{
    public class OutboxPipelineTests
    {
        private readonly DbContextOptions<OutboxDbContext> _options;

        public OutboxPipelineTests()
        {
            _options = new DbContextOptionsBuilder<OutboxDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
        }

        [Fact]
        public async Task ProcessAsync_ShouldExecuteAllSteps()
        {
            // Arrange
            using var context = new OutboxDbContext(_options);
            var steps = new List<IPipelineStep<OutboxMessage>>
            {
                new SaveToOutboxStep(context)
            };
            var pipeline = new OutboxPipeline<OutboxMessage>(steps);
            var message = new OutboxMessage
            {
                Id = Guid.NewGuid(),
                MessageType = "TestMessage",
                Payload = "Test Payload",
                Status = "Pending"
            };

            // Act
            await pipeline.ProcessAsync(message);

            // Assert
            var savedMessage = await context.OutboxMessages.FirstOrDefaultAsync(m => m.Id == message.Id);
            Assert.NotNull(savedMessage);
            Assert.Equal("TestMessage", savedMessage.MessageType);
            Assert.Equal("Test Payload", savedMessage.Payload);
            Assert.Equal("Pending", savedMessage.Status);
        }

        [Fact]
        public async Task ProcessAsync_ShouldExecuteStepsInOrder()
        {
            // Arrange
            var executionOrder = new List<string>();
            var step1 = new TestStep("Step1", executionOrder);
            var step2 = new TestStep("Step2", executionOrder);
            var steps = new List<IPipelineStep<OutboxMessage>> { step1, step2 };
            var pipeline = new OutboxPipeline<OutboxMessage>(steps);
            var message = new OutboxMessage 
            { 
                Id = Guid.NewGuid(),
                MessageType = "TestMessage",
                Payload = "Test Payload",
                Status = "Pending"
            };

            // Act
            await pipeline.ProcessAsync(message);

            // Assert
            Assert.Equal(new[] { "Step1", "Step2" }, executionOrder);
        }

        private class TestStep : IPipelineStep<OutboxMessage>
        {
            private readonly string _name;
            private readonly List<string> _executionOrder;

            public TestStep(string name, List<string> executionOrder)
            {
                _name = name;
                _executionOrder = executionOrder;
            }

            public Task ExecuteAsync(OutboxMessage message)
            {
                _executionOrder.Add(_name);
                return Task.CompletedTask;
            }
        }
    }
} 