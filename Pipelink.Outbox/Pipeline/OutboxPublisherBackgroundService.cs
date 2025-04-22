using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Pipelink.Outbox.Pipeline;

public class OutboxPublisherBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly OutboxPublisherOptions _options;
    private readonly ILogger<OutboxPublisherBackgroundService> _logger;

    public OutboxPublisherBackgroundService(
        IServiceProvider serviceProvider,
        OutboxPublisherOptions options,
        ILogger<OutboxPublisherBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingMessages(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while processing outbox messages");
            }

            await Task.Delay(_options.RetryInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingMessages(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OutboxDbContext>();
        var pipeline = scope.ServiceProvider.GetRequiredService<IOutboxPipeline<OutboxMessage>>();

        var pendingMessages = await dbContext.OutboxMessages
            .Where(m => !m.IsProcessed && m.RetryCount < _options.RetryCount && m.Status == "Pending")
            .OrderBy(m => m.CreatedAt)
            .Take(_options.BatchSize)
            .ToListAsync(stoppingToken);

        foreach (var message in pendingMessages)
        {
            try
            {
                await pipeline.ProcessAsync(message);

                message.IsProcessed = true;
                message.ProcessedAt = DateTime.UtcNow;
                message.Status = "Completed";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process message {MessageId}", message.Id);
                message.RetryCount++;
                message.LastError = ex.Message;
                message.LastErrorAt = DateTime.UtcNow;
                message.Status = message.RetryCount >= _options.RetryCount ? "Failed" : "Pending";
            }
        }

        if (pendingMessages.Any())
        {
            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
} 