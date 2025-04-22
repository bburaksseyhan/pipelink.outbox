using Pipelink.Outbox.Data;
using Pipelink.Outbox.Interfaces;
using Pipelink.Outbox.Models;

namespace Pipelink.Outbox.Steps
{
    /// <summary>
    /// A pipeline step responsible for saving an <see cref="OutboxMessage"/> into the database.
    /// This step writes the message to an outbox storage for further processing or distribution.
    /// </summary>
    public class SaveToOutboxStep : IPipelineStep<OutboxMessage>
    {
        /// <summary>
        /// Represents the database context for interacting with the outbox storage.
        /// </summary>
        /// <remarks>
        /// The _context variable is an instance of the OutboxDbContext class, which is used to manage
        /// the database operations for storing and retrieving OutboxMessage entities.
        /// It is employed within the SaveToOutboxStep to persist messages in the outbox with their
        /// relevant metadata, statuses, and timestamps.
        /// </remarks>
        private readonly OutboxDbContext _context;

        /// <summary>
        /// Represents a pipeline step responsible for persisting messages to the outbox database.
        /// </summary>
        public SaveToOutboxStep(OutboxDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Executes the pipeline step to save the given OutboxMessage to the outbox database.
        /// Sets the CreatedAt timestamp to the current UTC time and updates the message status to "Pending".
        /// </summary>
        /// <param name="message">The OutboxMessage containing the information to be saved.</param>
        /// <returns>A task that represents the asynchronous operation.</returns>
        public async Task ExecuteAsync(OutboxMessage message)
        {
            message.CreatedAt = DateTime.UtcNow;
            message.Status = "Pending";
            
            _context.OutboxMessages.Add(message);
            await _context.SaveChangesAsync();
        }
    }
} 