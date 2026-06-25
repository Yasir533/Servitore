namespace Servitore.API.Services;

public interface IEmailService
{
    Task SendAsync(string toAddress, string subject, string body);
}

// Stub SMTP sender — plug in MailKit/System.Net.Mail with credentials from
// configuration when ready for production use.
public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public Task SendAsync(string toAddress, string subject, string body)
    {
        _logger.LogInformation("[Email] To: {To}, Subject: {Subject}", toAddress, subject);
        return Task.CompletedTask;
    }
}
