using Finanzuebersicht.Core.AppLock;

namespace Finanzuebersicht.Presentation.Services.AppLock;

public sealed class AppLockEntryGate(IAppLockCoordinator coordinator)
{
    public async Task<bool> RunIfAllowedAsync(Func<Task> operation)
    {
        if (!coordinator.CanAccessContent)
            return false;
        await operation();
        return true;
    }
}
