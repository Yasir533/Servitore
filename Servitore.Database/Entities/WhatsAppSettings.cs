namespace Servitore.Database.Entities;

public class WhatsAppSettings
{
    public int Id { get; set; }
    public string PhoneNumber { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
}
