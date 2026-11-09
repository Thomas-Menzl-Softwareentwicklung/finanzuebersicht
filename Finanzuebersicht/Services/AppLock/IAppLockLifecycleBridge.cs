namespace Finanzuebersicht.Services.AppLock;

public interface IAppLockLifecycleBridge
{
    void Attach(Microsoft.Maui.Controls.Window window);
}
