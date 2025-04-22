using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Models;

namespace Pipelink.Outbox.Data
{
    /// <summary>
    /// Represents the Entity Framework Core database context for the Outbox pattern storage.
    /// </summary>
    /// <remarks>
    /// The OutboxDbContext manages the interaction with the database for storing and retrieving
    /// outbox messages used in the implementation of the Outbox pattern. It provides configuration
    /// for the database schema and manages the lifecycle of entity objects.
    /// </remarks>
    public class OutboxDbContext : DbContext
    {
        /// <summary>
        /// Represents the database context for managing outbox messages.
        /// </summary>
        /// <remarks>
        /// This context is used for interacting with the OutboxMessages table in the database.
        /// It is configured using Entity Framework Core.
        /// </remarks>
        public OutboxDbContext(DbContextOptions<OutboxDbContext> options)
            : base(options)
        {
        }

        /// <summary>
        /// Represents the collection of <c>OutboxMessage</c> entities within the database context.
        /// This property maps to a database table where messages are stored, enabling support for
        /// outbox pattern implementations. It allows querying, adding, updating, and deleting
        /// <c>OutboxMessage</c> records.
        /// </summary>
        public DbSet<OutboxMessage> OutboxMessages { get; set; }

        /// Configures the entity mappings for the context.
        /// This method is called when the model for a derived context is being created.
        /// It allows the configuration and mapping of entities to tables and other database-specific settings.
        /// <param name="modelBuilder">
        /// The builder used to construct the model for the context. Used to specify entity configurations.
        /// </param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OutboxMessage>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.MessageType).IsRequired();
                entity.Property(e => e.Payload).IsRequired();
                entity.Property(e => e.CreatedAt).IsRequired();
                entity.Property(e => e.Status).IsRequired();
            });
        }
    }
} 