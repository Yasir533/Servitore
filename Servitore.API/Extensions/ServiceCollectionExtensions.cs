using Servitore.API.Repositories;
using Servitore.API.Services;
using Servitore.Reports;

namespace Servitore.API.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddServitoreServices(this IServiceCollection services)
    {
        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICustomerRepository, CustomerRepository>();
        services.AddScoped<IServiceTicketRepository, ServiceTicketRepository>();
        services.AddScoped<IAssetRepository, AssetRepository>();
        services.AddScoped<IAMCVisitRepository, AMCVisitRepository>();
        services.AddScoped<IActivityLogRepository, ActivityLogRepository>();

        // Auth
        services.AddScoped<IAuthService, AuthService>();

        // Services
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IServiceTicketService, ServiceTicketService>();
        services.AddScoped<INotificationService, NotificationService>();
        services.AddScoped<IBarcodeService, BarcodeService>();
        services.AddScoped<IEmailService, EmailService>();
        services.AddScoped<IAMCVisitService, AMCVisitService>();
        services.AddScoped<IActivityLogService, ActivityLogService>();
        services.AddHttpClient<IWhatsAppService, WhatsAppService>();

        // Reports Exporter
        services.AddScoped<IExportService, ExportService>();

        return services;
    }
}
