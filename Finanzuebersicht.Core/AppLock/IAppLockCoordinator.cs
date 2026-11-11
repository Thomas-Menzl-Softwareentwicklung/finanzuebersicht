namespace Finanzuebersicht.Core.AppLock;

public interface IAppLockCoordinator
{
    AppLockState State { get; }
    bool CanAccessContent { get; }
    AuthenticationResult? LatestAuthenticationResult { get; }
    event Action? StateChanged;

    Task InitializeAsync(CancellationToken cancellationToken = default);
    void EnterBackground();
    Task ActivateAsync(string reason, CancellationToken cancellationToken = default);
    Task UnlockAsync(string reason, CancellationToken cancellationToken = default);
    Task<bool> ChangeOptionsAsync(AppLockOptions value, string reason, CancellationToken cancellationToken = default);
}
