using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Servitore.Database.Entities;

namespace Servitore.Database.Configurations;

public class ServiceTicketConfiguration : IEntityTypeConfiguration<ServiceTicket>
{
    public void Configure(EntityTypeBuilder<ServiceTicket> builder)
    {
        builder.HasKey(t => t.TicketId);
        builder.Property(t => t.TicketNumber).IsRequired().HasMaxLength(30);
        builder.HasIndex(t => t.TicketNumber).IsUnique();
        builder.Property(t => t.ProblemDescription).IsRequired().HasMaxLength(2000);

        builder.HasOne(t => t.Customer)
            .WithMany(c => c.ServiceTickets)
            .HasForeignKey(t => t.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Asset)
            .WithMany(a => a.ServiceTickets)
            .HasForeignKey(t => t.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany(u => u.CreatedTickets)
            .HasForeignKey(t => t.CreatedBy)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
