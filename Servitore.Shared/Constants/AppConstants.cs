namespace Servitore.Shared.Constants;

public static class AppConstants
{
    // API base URL (no trailing slash, no /api — endpoints include "api/..." prefix)
    public const string ApiBaseUrlSetting = "ApiBaseUrl";
    public const string DefaultApiBaseUrl = "https://localhost:5001";

    // SignalR
    public const string NotificationHubUrl = "/hubs/notifications";

    // Auth
    public const string JwtTokenStorageKey = "Servitore_AuthToken";
    public const int TokenExpiryMinutes = 480;

    // Ticket numbering
    public const string TicketNumberPrefix = "SVT";

    // Pagination
    public const int DefaultPageSize = 25;
}
