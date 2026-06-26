namespace Servitore.Database.Context;

public interface ICurrentUserService
{
    string? GetCurrentUsername();
}
