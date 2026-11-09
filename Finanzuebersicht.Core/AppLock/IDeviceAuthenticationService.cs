namespace Finanzuebersicht.Core.AppLock;

public interface IDeviceAuthenticationService
{
    Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);
    Task<AuthenticationResult> AuthenticateAsync(string reason, CancellationToken cancellationToken = default);
}
