using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");
        builder.HasKey(o => o.Id);
        builder.Property(o => o.EventType).IsRequired().HasMaxLength(200);
        builder.Property(o => o.Payload).IsRequired();
        builder.Property(o => o.Error).HasMaxLength(2000);
        builder.Property(o => o.CorrelationId).IsRequired();
        builder.Property(o => o.NextRetryAt);

        // Processor reads only unprocessed messages ordered by creation time
        builder.HasIndex(o => o.ProcessedAt);
        builder.HasIndex(o => new { o.ProcessedAt, o.NextRetryAt, o.CreatedAt });

        // Idempotency guard: same EventId is never inserted twice
        builder.HasIndex(o => o.EventId).IsUnique();

        // Troubleshooting: find all outbox events of a given HTTP request chain
        builder.HasIndex(o => o.CorrelationId);
    }
}
