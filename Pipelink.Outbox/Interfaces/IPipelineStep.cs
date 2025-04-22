namespace Pipelink.Outbox.Interfaces
{
    /// <summary>
    /// Represents a step in
    public interface IPipelineStep<TMessage>
    {
        /// <summary>
        /// Executes the pipeline step logic for the provided message.
        /// </summary>
        /// <param name="message">The message object to be processed by the pipeline step.</param>
        /// <returns>A task that represents the asynchronous execution of the pipeline step.</returns>
        Task ExecuteAsync(TMessage message);
    }
} 