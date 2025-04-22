using Pipelink.Outbox.Interfaces;

namespace Pipelink.Outbox.Pipeline
{
    /// <summary>
    /// Represents a pipeline for processing messages using a series of steps.
    /// </summary>
    /// <typeparam name="TMessage">The type of the message to be processed through the pipeline.</typeparam>
    public class OutboxPipeline<TMessage> : IOutboxPipeline<TMessage>
    {
        /// <summary>
        /// A private readonly collection of pipeline steps to be executed within the pipeline.
        /// Each step in the collection implements the <see cref="IPipelineStep{TMessage}"/> interface
        /// and represents a stage in the processing pipeline.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message to be processed by the pipeline steps.</typeparam>
        private readonly IEnumerable<IPipelineStep<TMessage>> _steps;

        /// <summary>
        /// Represents a pipeline pattern implementation for processing messages in an outbox system.
        /// This class iterates over a sequence of steps and processes the provided message asynchronously.
        /// </summary>
        /// <typeparam name="TMessage">The type of the message being processed in the pipeline.</typeparam>
        public OutboxPipeline(IEnumerable<IPipelineStep<TMessage>> steps)
        {
            _steps = steps;
        }

        /// <summary>
        /// Processes the given message through all configured pipeline steps in order.
        /// </summary>
        /// <param name="message">The message to be processed by the pipeline.</param>
        /// <returns>A task that represents the asynchronous operation of processing the message.</returns>
        public async Task ProcessAsync(TMessage message)
        {
            foreach (var step in _steps)
            {
                await step.ExecuteAsync(message);
            }
        }
    }
} 