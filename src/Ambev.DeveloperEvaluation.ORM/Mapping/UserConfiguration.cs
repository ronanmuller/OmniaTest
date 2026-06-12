using Ambev.DeveloperEvaluation.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Ambev.DeveloperEvaluation.ORM.Mapping;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).HasColumnType("uuid").HasDefaultValueSql("gen_random_uuid()");

        builder.Property(u => u.Username).IsRequired().HasMaxLength(50);
        builder.Property(u => u.Password).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Email).IsRequired().HasMaxLength(100);
        builder.Property(u => u.Phone).HasMaxLength(20);
        builder.Property(u => u.Firstname).HasMaxLength(50);
        builder.Property(u => u.Lastname).HasMaxLength(50);

        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(u => u.Role).HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(u => u.Email).IsUnique();
        builder.HasIndex(u => u.Username);

        builder.OwnsOne(u => u.Address, addr =>
        {
            addr.Property(a => a.City).HasColumnName("City").HasMaxLength(100);
            addr.Property(a => a.Street).HasColumnName("Street").HasMaxLength(200);
            addr.Property(a => a.Number).HasColumnName("Number");
            addr.Property(a => a.Zipcode).HasColumnName("Zipcode").HasMaxLength(20);
            addr.Property(a => a.Lat).HasColumnName("Lat").HasMaxLength(50);
            addr.Property(a => a.Long).HasColumnName("Long").HasMaxLength(50);
        });
    }
}
