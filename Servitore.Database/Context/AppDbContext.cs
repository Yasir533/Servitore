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
    public DbSet<ServiceTicket> ServiceTickets => Set<ServiceTicket>();
    public DbSet<TicketHistory> TicketHistories => Set<TicketHistory>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
    public DbSet<Warranty> Warranties => Set<Warranty>();
    public DbSet<AMCContract> AMCContracts => Set<AMCContract>();
    public DbSet<Notification> Notifications => Set<Notification>();
    public DbSet<Settings> Settings => Set<Settings>();
    public DbSet<WhatsAppSettings> WhatsAppSettings => Set<WhatsAppSettings>();
    public DbSet<AssetDocument> AssetDocuments => Set<AssetDocument>();
    public DbSet<AMCVisit> AMCVisits => Set<AMCVisit>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Configure relations for extended features
        modelBuilder.Entity<ServiceTicket>()
            .HasOne(t => t.AssignedToUser)
            .WithMany(u => u.AssignedTickets)
            .HasForeignKey(t => t.AssignedToUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AssetDocument>()
            .HasOne(d => d.Asset)
            .WithMany(a => a.Documents)
            .HasForeignKey(d => d.AssetId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AMCVisit>()
            .HasOne(v => v.AMCContract)
            .WithMany(c => c.Visits)
            .HasForeignKey(v => v.AMCContractId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<AMCVisit>()
            .HasOne(v => v.Engineer)
            .WithMany()
            .HasForeignKey(v => v.EngineerId)
            .OnDelete(DeleteBehavior.Restrict);

        // Seed default roles
        modelBuilder.Entity<Role>().HasData(
            new Role { RoleId = 1, RoleName = "Admin" },
            new Role { RoleId = 2, RoleName = "Manager" },
            new Role { RoleId = 3, RoleName = "Engineer" },
            new Role { RoleId = 4, RoleName = "Operator" }
        );

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
