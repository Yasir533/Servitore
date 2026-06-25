using Microsoft.EntityFrameworkCore;
using Servitore.Database.Context;
using Servitore.Database.Entities;

namespace Servitore.Database;

public static class SeedData
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Roles are seeded via HasData in OnModelCreating (handled by migration).
        // Here we seed the default admin user if it doesn't exist yet.

        if (!await context.Users.AnyAsync(u => u.Username == "admin"))
        {
            // RoleId = 1 = Admin (seeded in OnModelCreating)
            var adminUser = new User
            {
                Username = "admin",
                FullName = "System Administrator",
                Email = "admin@servitore.local",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
                RoleId = 1,
                IsActive = true,
                CreatedDate = DateTime.UtcNow
            };

            context.Users.Add(adminUser);
            await context.SaveChangesAsync();
        }
    }
}
