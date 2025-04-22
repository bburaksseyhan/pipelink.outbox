using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Pipelink.Outbox.Pipeline;

public class OutboxPublisher : IOutboxPublisher
{
    private readonly OutboxDbContext _dbContext;
    private readonly IOutboxPipeline<OutboxMessage> _pipeline;

    public OutboxPublisher(OutboxDbContext dbContext, IOutboxPipeline<OutboxMessage> pipeline)
    {
        _dbContext = dbContext;
        _pipeline = pipeline;
    }

    public async Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class
    {
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            MessageType = typeof(T).FullName ?? typeof(T).Name,
            Payload = JsonSerializer.Serialize(message),
            CreatedAt = DateTime.UtcNow,
            IsProcessed = false,
            RetryCount = 0,
            Status = "Pending"
        };

        await _dbContext.OutboxMessages.AddAsync(outboxMessage, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task ProcessPendingMessagesAsync(CancellationToken cancellationToken = default)
    {
        var pendingMessages = await _dbContext.OutboxMessages
            .Where(m => !m.IsProcessed && m.Status == "Pending")
            .ToListAsync(cancellationToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                await _pipeline.ProcessAsync(message);

                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.Status = "Completed";
            }
            catch (Exception ex)
            {
                message.RetryCount++;
                message.LastError = ex.Message;
                message.LastErrorAt = DateTime.UtcNow;
                message.Status = message.RetryCount >= 3 ? "Failed" : "Pending";
            }
        }

        if (pendingMessages.Any())
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
    }
} 