using Microsoft.EntityFrameworkCore;
using Servitore.Database.Entities;

namespace Servitore.Database.Context;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService? currentUserService = null) 
        : base(options) 
    {
        _currentUserService = currentUserService;
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<ServiceEntry> ServiceEntries => Set<ServiceEntry>();
    public DbSet<ServiceEntryHistory> ServiceEntryHistories => Set<ServiceEntryHistory>();
    public DbSet<ServiceEntryAttachment> ServiceEntryAttachments => Set<ServiceEntryAttachment>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<WhatsAppSettings> WhatsAppSettings => Set<WhatsAppSettings>();
    public DbSet<AssetDocument> AssetDocuments => Set<AssetDocument>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure relations for extended features
        modelBuilder.Entity<ServiceEntry>()
            .HasOne(e => e.AssignedToUser)
            .WithMany(u => u.AssignedEntries)
            .HasForeignKey(e => e.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetDocument>()
            .HasOne(d => d.Asset)
            .WithMany(a => a.Documents)
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ServiceEntryAttachment>()
            .HasOne(a => a.ServiceEntry)
            .WithMany(e => e.Attachments)
            .HasForeignKey(a => a.ServiceEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Manager" },
            new Role { RoleId = 3, RoleName = "Engineer" },
            new Role { RoleId = 4, RoleName = "Operator" }
        );

        // Soft delete query filters
        modelBuilder.Entity<Customer>().HasQueryFilter(c => !c.IsDeleted);
        modelBuilder.Entity<Asset>().HasQueryFilter(a => !a.IsDeleted);
        modelBuilder.Entity<ServiceEntry>().HasQueryFilter(e => !e.IsDeleted);

        base.OnModelCreating(modelBuilder);
    }

    public override System.Threading.Tasks.Task<int> SaveChangesAsync(System.Threading.CancellationToken cancellationToken = default)
    {
        var entries = ChangeTracker
            .Entries()
            .Where(e => e.Entity is IAuditable && (e.State == EntityState.Added || e.State == EntityState.Modified));

        var username = _currentUserService?.GetCurrentUsername() ?? "System";
        var now = DateTime.UtcNow;

        foreach (var entityEntry in entries)
        {
            var auditable = (IAuditable)entityEntry.Entity;
            if (entityEntry.State == EntityState.Added)
            {
                auditable.CreatedBy = username;
                auditable.CreatedDate = now;
            }

            auditable.ModifiedBy = username;
            auditable.ModifiedDate = now;
        }

        return base.SaveChangesAsync(cancellationToken);
    }
}
