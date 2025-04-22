using System;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Models;
using Xunit;

namespace Pipelink.Outbox.Tests.Integration
{
    public class OutboxIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly string _dbName;

        public OutboxIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _dbName = Guid.NewGuid().ToString();
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<OutboxDbContext>));

                    if (descriptor != null)
                    {
                        services.Remove(descriptor);
                    }

                    services.AddDbContext<OutboxDbContext>(options =>
                    {
                        options.UseInMemoryDatabase(_dbName);
                    });
                });
            });
        }

        [Fact]
        public async Task CreateMessage_ShouldReturnSuccessAndStoreMessage()
        {
            // Arrange
            var client = _factory.CreateClient();
            var request = new
            {
                MessageType = "IntegrationTest",
                Payload = "Test Message Content"
            };

            // Act
            var response = await client.PostAsJsonAsync("/api/outbox", request);
            var content = await response.Content.ReadFromJsonAsync<CreateMessageResponse>();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(content);
            Assert.NotEqual(Guid.Empty, content.MessageId);

            // Verify the message was stored
            using var scope = _factory.Services.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
            var storedMessage = await context.OutboxMessages
                .FirstOrDefaultAsync(m => m.Id == content.MessageId);

            Assert.NotNull(storedMessage);
            Assert.Equal("IntegrationTest", storedMessage.MessageType);
            Assert.Equal("Test Message Content", storedMessage.Payload);
            Assert.Equal("Pending", storedMessage.Status);
        }

        [Fact]
        public async Task GetMessages_ShouldReturnAllMessages()
        {
            // Arrange
            using (var scope = _factory.Services.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
                await context.OutboxMessages.AddRangeAsync(
                    new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageType = "Test1",
                        Payload = "Payload1",
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    },
                    new OutboxMessage
                    {
                        Id = Guid.NewGuid(),
                        MessageType = "Test2",
                        Payload = "Payload2",
                        Status = "Pending",
                        CreatedAt = DateTime.UtcNow
                    }
                );
                await context.SaveChangesAsync();
            }

            var client = _factory.CreateClient();

            // Act
            var response = await client.GetAsync("/api/outbox");
            var messages = await response.Content.ReadFromJsonAsync<OutboxMessage[]>();

            // Assert
            Assert.True(response.IsSuccessStatusCode);
            Assert.NotNull(messages);
            Assert.Equal(2, messages.Length);
            Assert.Contains(messages, m => m.MessageType == "Test1");
            Assert.Contains(messages, m => m.MessageType == "Test2");
        }

        private class CreateMessageResponse
        {
            public Guid MessageId { get; set; }
        }
    }
} 