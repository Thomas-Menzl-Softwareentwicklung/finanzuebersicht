using Finanzuebersicht.Core.AppLock;

namespace Finanzuebersicht.Services.AppLock;

public sealed class UnavailableDeviceAuthenticationService : IDeviceAuthenticationService
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        => Task.FromResult(false);

    public Task<AuthenticationResult> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
        => Task.FromResult(AuthenticationResult.Unavailable);
}
