using System.Text.RegularExpressions;

namespace Servitore.Desktop.Helpers;

public static class ValidationHelper
{
    private static readonly Regex EmailRegex = new(@"^[^@\s]+@[^@\s]+\.[^@\s]+$", RegexOptions.Compiled);
    private static readonly Regex MobileRegex = new(@"^\+?[0-9]{7,15}$", RegexOptions.Compiled);

    public static bool IsValidEmail(string? email) =>
        !string.IsNullOrWhiteSpace(email) && EmailRegex.IsMatch(email);

    public static bool IsValidMobile(string? mobile) =>
        !string.IsNullOrWhiteSpace(mobile) && MobileRegex.IsMatch(mobile);

    public static bool IsRequired(string? value) => !string.IsNullOrWhiteSpace(value);
}
