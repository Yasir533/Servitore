namespace Servitore.Shared.Constants;

public static class AppConstants
{
    // API base URL (no trailing slash, no /api — endpoints include "api/..." prefix)
    public const string ApiBaseUrlSetting = "ApiBaseUrl";
#if DEBUG
    public const string DefaultApiBaseUrl = "http://localhost:5000";
#else
    public const string DefaultApiBaseUrl = "https://localhost:5001";
#endif

    // SignalR
    public const string NotificationHubUrl = "/hubs/collaboration";

    // Auth
    public const string JwtTokenStorageKey = "Servitore_AuthToken";
    public const int TokenExpiryMinutes = 480;

    // Ticket numbering
    public const string TicketNumberPrefix = "SVT";

    // Pagination
    public const int DefaultPageSize = 25;
}
