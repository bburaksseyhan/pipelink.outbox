using Microsoft.EntityFrameworkCore;
using Pipelink.Outbox.Models;

namespace Pipelink.Outbox.Tests.Data
{
    public class OutboxDbContext : DbContext
    {
        public OutboxDbContext(DbContextOptions<OutboxDbContext> options)
            : base(options)
        {
        }

        public DbSet<OutboxMessage> OutboxMessages { get; set; }

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