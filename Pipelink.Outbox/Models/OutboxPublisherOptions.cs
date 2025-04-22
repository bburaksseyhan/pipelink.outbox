using System;

namespace Pipelink.Outbox.Models;

public class OutboxPublisherOptions
{
    /// <summary>
    /// Number of retry attempts for failed messages (default: 3)
    /// </summary>
    public int RetryCount { get; set; } = 3;

    /// <summary>
    /// Time between retry attempts (default: 5 seconds)
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Number of messages to process in a single batch (default: 100)
    /// </summary>
    public int BatchSize { get; set; } = 100;
} 