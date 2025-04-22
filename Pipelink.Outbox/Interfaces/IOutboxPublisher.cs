using System.Threading;
using System.Threading.Tasks;

namespace Pipelink.Outbox.Interfaces;

/// <summary>
/// Defines the contract for publishing messages through the outbox pattern.
/// </summary>
public interface IOutboxPublisher
{
    /// <summary>
    /// Publishes a message to the outbox.
    /// </summary>
    /// <typeparam name="T">The type of the message.</typeparam>
    /// <param name="message">The message to publish.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task PublishAsync<T>(T message, CancellationToken cancellationToken = default) where T : class;
} 