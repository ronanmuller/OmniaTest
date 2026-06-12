using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class ProcessedMessageConfiguration : IEntityTypeConfiguration<ProcessedMessage>
{
    public void Configure(EntityTypeBuilder<ProcessedMessage> builder)
    {
        builder.ToTable("processed_messages");
        builder.HasKey(p => new { p.MessageId, p.Consumer });
        builder.Property(p => p.Consumer).IsRequired().HasMaxLength(200);
    }
}
