using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Servitore.API.Extensions;
using Servitore.API.Middleware;
using Servitore.API.SignalR;
using Servitore.Database;
using Servitore.Database.Context;
using Servitore.API.Services;

var builder = WebApplication.CreateBuilder(args);

// Load optional databaseSettings.json file
builder.Configuration.AddJsonFile("databaseSettings.json", optional: true, reloadOnChange: true);

// Startup Configuration Validation
string? connectionString;
var dbSettings = builder.Configuration.GetSection("DatabaseSettings");
if (dbSettings.Exists() && !string.IsNullOrWhiteSpace(dbSettings["Server"]) && !string.IsNullOrWhiteSpace(dbSettings["Database"]))
{
    var server = dbSettings["Server"];
    var database = dbSettings["Database"];
    var username = dbSettings["Username"];
    var password = dbSettings["Password"];

    if (string.IsNullOrWhiteSpace(username))
    {
        connectionString = $"Server={server};Database={database};Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }
    else
    {
        connectionString = $"Server={server};Database={database};User Id={username};Password={password};TrustServerCertificate=True;MultipleActiveResultSets=True;";
    }
}
else
{
    connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
}

if (string.IsNullOrWhiteSpace(connectionString))
{
    throw new InvalidOperationException("CRITICAL CONFIGURATION ERROR: Connection string is missing or empty. Please configure databaseSettings.json or ConnectionStrings:DefaultConnection in appsettings.json.");
}

var jwtKey = builder.Configuration["Jwt:Key"];
if (string.IsNullOrWhiteSpace(jwtKey))
{
    throw new InvalidOperationException("CRITICAL CONFIGURATION ERROR: Jwt:Key is missing or empty.");
}

if (!builder.Environment.IsDevelopment())
{
    if (jwtKey == "Serv!t0re@JWT#SecretKey$2024%Secure&Long!Enough")
    {
        throw new InvalidOperationException("CRITICAL SECURITY ERROR: The JWT secret key in Production matches the development placeholder! Update Jwt:Key in environment variables.");
    }
    if (jwtKey.Length < 32)
    {
        throw new InvalidOperationException("CRITICAL SECURITY ERROR: The JWT secret key is too short. It must be at least 256 bits (32 characters) for security.");
    }
}

// Database
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(connectionString));

// App services / repositories (see ServiceCollectionExtensions)
builder.Services.AddServitoreServices();
builder.Services.AddHostedService<StaleLockCleanupService>();
builder.Services.AddHostedService<SoftDeleteCleanupService>();

// SignalR for real-time multi-desktop sync
builder.Services.AddSignalR();

// JWT authentication
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };

        // Allow SignalR to receive the JWT via query string for hub connections
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                var path = context.HttpContext.Request.Path;
                if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddCors(options =>
{
    options.AddPolicy("DesktopClients", policy =>
    {
        policy.AllowAnyHeader().AllowAnyMethod().SetIsOriginAllowed(_ => true).AllowCredentials();
    });
});

var app = builder.Build();

// ── Run migrations and seed data in background task ──────────────────────────
_ = Task.Run(async () =>
{
    using (var scope = app.Services.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        try
        {
            db.Database.Migrate();                          // applies pending migrations
            await SeedData.SeedAsync(db);                  // seeds admin user if absent
        }
        catch (Exception ex)
        {
            var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
            logger.LogError(ex, "An error occurred while migrating or seeding the database.");
        }
    }
});
// ─────────────────────────────────────────────────────────────────────────────

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<ExceptionMiddleware>();
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}
app.UseCors("DesktopClients");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<CollaborationHub>("/hubs/collaboration");

app.Run();
