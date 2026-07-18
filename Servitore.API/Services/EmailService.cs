using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Servitore.API.Services;

public interface IEmailService
{
    Task SendAsync(string toAddress, string subject, string body);
}

public class EmailService : IEmailService
{
    private readonly ILogger<EmailService> _logger;
    private readonly IConfiguration _configuration;

    public EmailService(ILogger<EmailService> logger, IConfiguration configuration)
    {
        _logger = logger;
        _configuration = configuration;
    }

    public async Task SendAsync(string toAddress, string subject, string body)
    {
        _logger.LogInformation("[Email] Sending email to {To}. Subject: {Subject}", toAddress, subject);

        var smtpHost = _configuration["Smtp:Host"];
        var smtpPortStr = _configuration["Smtp:Port"];
        var smtpUser = _configuration["Smtp:Username"];
        var smtpPass = _configuration["Smtp:Password"];
        var fromAddress = _configuration["Smtp:FromAddress"] ?? "noreply@servitore.local";

        if (string.IsNullOrWhiteSpace(smtpHost))
        {
            _logger.LogWarning("[Email] SMTP Host is not configured in appsettings.json. Falling back to logging details.");
            _logger.LogInformation("[Email Body]\n{Body}", body);
            return;
        }

        int smtpPort = int.TryParse(smtpPortStr, out var p) ? p : 587;

        try
        {
            using var client = new SmtpClient(smtpHost, smtpPort)
            {
                EnableSsl = true
            };

            if (!string.IsNullOrWhiteSpace(smtpUser) && !string.IsNullOrWhiteSpace(smtpPass))
            {
                client.Credentials = new NetworkCredential(smtpUser, smtpPass);
            }

            var mailMessage = new MailMessage
            {
                From = new MailAddress(fromAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toAddress);

            await client.SendMailAsync(mailMessage);
            _logger.LogInformation("[Email] Email sent successfully via SMTP.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[Email] Failed to send email via SMTP to {To}.", toAddress);
            throw;
        }
    }
}
