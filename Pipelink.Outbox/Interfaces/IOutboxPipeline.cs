namespace Pipelink.Outbox.Interfaces
{
    /// <summary>
    /// Represents a pipeline for processing messages within the Outbox pattern.
    /// </summary>
    /// <typeparam name="TMessage">The type of message that the pipeline processes.</typeparam>
    public interface IOutboxPipeline<TMessage>
    {
        /// <summary>
        /// Processes a message through the outbox pipeline by executing each step in the pipeline.
        /// </summary>
        /// <param name="message">The message to be processed by the pipeline.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task ProcessAsync(TMessage message);
    }
} 