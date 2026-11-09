using Finanzuebersicht.Core.AppLock;

namespace Finanzuebersicht.Presentation.Services.AppLock;

/// <summary>
/// Starts the session timeout only after every known Apple scene is in the background,
/// and resumes the session once when the first scene returns to the foreground.
/// </summary>
public sealed class AppLockSceneRegistry(IAppLockCoordinator coordinator)
{
    private readonly Dictionary<object, bool> _scenesInBackground = new();
    private bool _backgroundTransitionRecorded;

    public async Task RegisterAsync(object scene, bool isInForeground, string reason, CancellationToken cancellationToken = default)
    {
        if (_scenesInBackground.ContainsKey(scene))
        {
            await SetInForegroundAsync(scene, isInForeground, reason, cancellationToken);
            return;
        }

        var wasAllInBackground = _scenesInBackground.Count > 0 && AreAllInBackground();
        _scenesInBackground.Add(scene, !isInForeground);
        await ApplyTransitionAsync(wasAllInBackground, isInForeground, reason, cancellationToken);
    }

    public async Task SetInForegroundAsync(object scene, bool isInForeground, string reason, CancellationToken cancellationToken = default)
    {
        if (!_scenesInBackground.TryGetValue(scene, out var wasInBackground))
        {
            await RegisterAsync(scene, isInForeground, reason, cancellationToken);
            return;
        }

        var wasAllInBackground = AreAllInBackground();
        var isInBackground = !isInForeground;
        if (wasInBackground == isInBackground)
            return;

        _scenesInBackground[scene] = isInBackground;
        await ApplyTransitionAsync(wasAllInBackground, isInForeground, reason, cancellationToken);
    }

    public void Unregister(object scene)
    {
        var wasAllInBackground = _scenesInBackground.Count > 0 && AreAllInBackground();
        if (!_scenesInBackground.Remove(scene))
            return;

        var isAllInBackground = AreAllInBackground();
        if (!wasAllInBackground && isAllInBackground)
            MarkBackground();
    }

    private async Task ApplyTransitionAsync(bool wasAllInBackground, bool isInForeground, string reason, CancellationToken cancellationToken)
    {
        var isAllInBackground = AreAllInBackground();
        if (!wasAllInBackground && isAllInBackground)
            MarkBackground();
        else if (_backgroundTransitionRecorded && isInForeground && !isAllInBackground)
        {
            _backgroundTransitionRecorded = false;
            await coordinator.ActivateAsync(reason, cancellationToken);
        }
    }

    private void MarkBackground()
    {
        _backgroundTransitionRecorded = true;
        coordinator.EnterBackground();
    }

    private bool AreAllInBackground()
        => _scenesInBackground.Count == 0 || _scenesInBackground.Values.All(value => value);
}
