using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");

        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(c => c.UserId).IsRequired();
        builder.Property(c => c.Date).IsRequired();

        builder.HasMany(c => c.Products)
            .WithOne()
            .HasForeignKey(i => i.CartId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(c => c.UserId);
        builder.HasIndex(c => c.Date);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();
    }
}
