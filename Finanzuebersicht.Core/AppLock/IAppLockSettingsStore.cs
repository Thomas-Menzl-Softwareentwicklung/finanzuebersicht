namespace Finanzuebersicht.Core.AppLock;

public interface IAppLockSettingsStore
{
    Task<AppLockOptions> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(AppLockOptions value, CancellationToken cancellationToken = default);
}
