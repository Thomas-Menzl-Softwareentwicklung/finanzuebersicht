using Finanzuebersicht.Helpers;

namespace Finanzuebersicht.Tests.AppLock
{
    public sealed class DesktopModalLockTests : IDisposable
    {
        private readonly global::Finanzuebersicht.App _app = new();
        private string _draft = "Unfinished expense";
        private int _dismissals;

        public DesktopModalLockTests()
        {
            Microsoft.Maui.Controls.Application.Current = _app;
            DesktopMenuBridge.DismissModalAsync = () =>
            {
                _dismissals++;
                _draft = string.Empty;
                return Task.CompletedTask;
            };
        }

        [Fact]
        public void EscapeWhileLocked_PreservesOpenForm()
        {
            _app.AppLockCanAccessContent = false;

            DesktopMenuBridge.DismissModal();
            MainThread.Drain();

            Assert.Equal("Unfinished expense", _draft);
            Assert.Equal(0, _dismissals);
        }

        [Fact]
        public void LockBeforeQueuedEscapeRuns_PreservesOpenForm()
        {
            _app.AppLockCanAccessContent = true;
            DesktopMenuBridge.DismissModal();
            _app.AppLockCanAccessContent = false;

            MainThread.Drain();

            Assert.Equal("Unfinished expense", _draft);
            Assert.Equal(0, _dismissals);
        }

        [Fact]
        public void EscapeAfterUnlock_DismissesOpenFormOnce()
        {
            _app.AppLockCanAccessContent = false;
            DesktopMenuBridge.DismissModal();
            MainThread.Drain();
            _app.AppLockCanAccessContent = true;

            DesktopMenuBridge.DismissModal();
            MainThread.Drain();

            Assert.Empty(_draft);
            Assert.Equal(1, _dismissals);
        }

        public void Dispose()
        {
            DesktopMenuBridge.DismissModalAsync = null;
            Microsoft.Maui.Controls.Application.Current = null;
            MainThread.Clear();
        }
    }
}

// Only the platform boundaries are replaced. DesktopMenuBridge runs unchanged,
// including its queued UI dispatch and the access check against Application.Current.
namespace Microsoft.Maui.Controls
{
    internal class Application
    {
        public static Application? Current { get; set; }
    }
}

namespace Finanzuebersicht
{
    internal sealed class App : Microsoft.Maui.Controls.Application
    {
        public bool AppLockCanAccessContent { get; set; }
    }
}

namespace Finanzuebersicht.Helpers
{
    internal static class MainThread
    {
        private static readonly Queue<Action> Pending = new();
        public static void BeginInvokeOnMainThread(Action action) => Pending.Enqueue(action);
        public static void Drain()
        {
            while (Pending.TryDequeue(out var action))
                action();
        }
        public static void Clear() => Pending.Clear();
    }
}
