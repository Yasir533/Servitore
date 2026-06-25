using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servitore.Database.Entities;

namespace Servitore.Database.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.HasKey(c => c.CustomerId);
        builder.Property(c => c.CustomerName).IsRequired().HasMaxLength(200);
        builder.Property(c => c.Mobile).HasMaxLength(20);
        builder.Property(c => c.Email).HasMaxLength(150);
    }
}
