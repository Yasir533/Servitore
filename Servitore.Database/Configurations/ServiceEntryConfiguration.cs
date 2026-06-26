using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servitore.Database.Entities;

namespace Servitore.Database.Configurations;

public class ServiceEntryConfiguration : IEntityTypeConfiguration<ServiceEntry>
{
    public void Configure(EntityTypeBuilder<ServiceEntry> builder)
    {
        builder.HasKey(e => e.ServiceEntryId);
        builder.Property(e => e.ServiceEntryNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(e => e.ServiceEntryNumber).IsUnique();
        builder.Property(e => e.ProblemDescription).IsRequired().HasMaxLength(2000);

        builder.HasOne(e => e.Customer)
            .WithMany(c => c.ServiceEntries)
            .HasForeignKey(e => e.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Asset)
            .WithMany(a => a.ServiceEntries)
            .HasForeignKey(e => e.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.CreatedByUser)
            .WithMany(u => u.CreatedEntries)
            .HasForeignKey(e => e.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
