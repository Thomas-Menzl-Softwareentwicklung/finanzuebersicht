namespace Finanzuebersicht.Services.AppLock;

public sealed class UnavailableAppLockLifecycleBridge(Finanzuebersicht.Core.AppLock.IAppLockCoordinator coordinator) : IAppLockLifecycleBridge
{
    public void Attach(Microsoft.Maui.Controls.Window window) => _ = coordinator.InitializeAsync();
}
