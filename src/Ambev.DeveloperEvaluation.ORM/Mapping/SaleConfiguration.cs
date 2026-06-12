using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("Sales");

        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(s => s.SaleNumber).IsRequired().HasMaxLength(20);
        builder.Property(s => s.CustomerName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.BranchName).IsRequired().HasMaxLength(100);
        builder.Property(s => s.TotalAmount).HasColumnType("numeric(18,2)");
        builder.Property(s => s.Date).IsRequired();

        builder.HasMany(s => s.Items)
            .WithOne()
            .HasForeignKey(i => i.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(s => s.SaleNumber).IsUnique();
        builder.HasIndex(s => s.Date);
        builder.HasIndex(s => s.CustomerId);
    }
}
