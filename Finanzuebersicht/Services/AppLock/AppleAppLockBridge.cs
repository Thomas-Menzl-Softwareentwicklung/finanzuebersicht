#if IOS || MACCATALYST
using Finanzuebersicht.Core.AppLock;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Presentation.Services.AppLock;
using Finanzuebersicht.Resources.Strings;
using Foundation;
using UIKit;

namespace Finanzuebersicht.Services.AppLock;

public sealed class AppleAppLockBridge : IAppLockLifecycleBridge, IDisposable
{
    private readonly IAppLockCoordinator _coordinator;
    private readonly AppLockSceneRegistry _sceneRegistry;
    private readonly ILocalizationService _localization;
    private readonly SemaphoreSlim _initializationGate = new(1, 1);
    private readonly Dictionary<UIWindowScene, SceneShield> _shields = new();
    private readonly Dictionary<Microsoft.Maui.Controls.Window, UIWindowScene> _scenesByWindow = new();
    private bool _initialized;
    private bool _initialUnlockAttempted;

    public AppleAppLockBridge(
        IAppLockCoordinator coordinator,
        AppLockSceneRegistry sceneRegistry,
        ILocalizationService localization)
    {
        _coordinator = coordinator;
        _sceneRegistry = sceneRegistry;
        _localization = localization;
        _coordinator.StateChanged += OnCoordinatorStateChanged;
    }

    public void Attach(Microsoft.Maui.Controls.Window window)
    {
        if (window.Handler?.PlatformView is not UIWindow hostWindow || hostWindow.WindowScene is not { } scene)
            return;

        if (_scenesByWindow.ContainsKey(window))
            return;

        _scenesByWindow.Add(window, scene);
        window.Destroying += (_, _) => Detach(window);
        if (!_shields.ContainsKey(scene))
        {
            var shield = CreateShieldWindow(hostWindow, scene);
            shield.IsActive = scene.ActivationState == UISceneActivationState.ForegroundActive;
            shield.IsInForeground = scene.ActivationState != UISceneActivationState.Background;
            _shields.Add(scene, shield);
            ShowShield(shield, locked: false);
            ObserveScene(shield);
        }

        _ = AttachAsync(window, scene);
    }

    public void Detach(Microsoft.Maui.Controls.Window window)
    {
        if (!_scenesByWindow.Remove(window, out var scene))
            return;

        if (_scenesByWindow.Values.Any(existing => existing.Handle == scene.Handle))
            return;

        if (_shields.Remove(scene, out var shield))
            DisposeShield(shield);
        _sceneRegistry.Unregister(scene);
    }

    public void Dispose()
    {
        _coordinator.StateChanged -= OnCoordinatorStateChanged;
        foreach (var shield in _shields.Values)
            DisposeShield(shield);
        _shields.Clear();
        _scenesByWindow.Clear();
        _initializationGate.Dispose();
    }

    private async Task AttachAsync(Microsoft.Maui.Controls.Window window, UIWindowScene scene)
    {
        try
        {
            await _initializationGate.WaitAsync();
            var initializedHere = false;
            try
            {
                if (!_initialized)
                {
                    await _coordinator.InitializeAsync();
                    _initialized = true;
                    initializedHere = true;
                }

                if (initializedHere
                    && !_initialUnlockAttempted
                    && _coordinator.State == AppLockState.Locked
                    && scene.ActivationState != UISceneActivationState.Background)
                {
                    _initialUnlockAttempted = true;
                    await _coordinator.UnlockAsync(GetUnlockReason());
                }
            }
            finally
            {
                _initializationGate.Release();
            }

            if (!_scenesByWindow.TryGetValue(window, out var attachedScene) || attachedScene.Handle != scene.Handle)
                return;
            await _sceneRegistry.RegisterAsync(
                scene,
                isInForeground: scene.ActivationState != UISceneActivationState.Background,
                GetUnlockReason());
            RefreshShield(scene);
        }
        catch (Exception)
        {
            if (_shields.TryGetValue(scene, out var shield))
            {
                shield.ErrorMessage = _localization.GetString(ResourceKeys.AppLock_SettingsLoadError);
                shield.ErrorLabel.Text = shield.ErrorMessage;
                ShowShield(shield, locked: true);
            }
        }
    }

    private SceneShield CreateShieldWindow(UIWindow hostWindow, UIWindowScene scene)
    {
        var controller = new UIViewController();
        var view = new UIView { BackgroundColor = UIColor.SystemBackground };
        view.AccessibilityViewIsModal = true;
        var title = new UILabel
        {
            Text = "Finanzübersicht",
            Font = UIFont.PreferredHeadline,
            TextAlignment = UITextAlignment.Center,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        var unlock = new UIButton(UIButtonType.System);
        unlock.SetTitle(GetUnlockLabel(), UIControlState.Normal);
        unlock.TitleLabel!.Font = UIFont.PreferredHeadline;
        unlock.TranslatesAutoresizingMaskIntoConstraints = false;
        unlock.TouchUpInside += OnUnlockTapped;
        var error = new UILabel
        {
            TextAlignment = UITextAlignment.Center,
            Lines = 0,
            Hidden = true,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        var stack = new UIStackView([title, unlock, error])
        {
            Axis = UILayoutConstraintAxis.Vertical,
            Alignment = UIStackViewAlignment.Center,
            Spacing = 20,
            TranslatesAutoresizingMaskIntoConstraints = false
        };
        view.AddSubview(stack);
        NSLayoutConstraint.ActivateConstraints([
            stack.CenterXAnchor.ConstraintEqualTo(view.CenterXAnchor),
            stack.CenterYAnchor.ConstraintEqualTo(view.CenterYAnchor),
            stack.LeadingAnchor.ConstraintGreaterThanOrEqualTo(view.SafeAreaLayoutGuide.LeadingAnchor, 24),
            stack.TrailingAnchor.ConstraintLessThanOrEqualTo(view.SafeAreaLayoutGuide.TrailingAnchor, -24)
        ]);
        controller.View = view;

        var shieldWindow = new UIWindow(scene)
        {
            RootViewController = controller,
            WindowLevel = UIWindowLevel.Normal + 1,
            Hidden = true,
            AccessibilityViewIsModal = true
        };
        return new SceneShield(this, scene, hostWindow, shieldWindow, view, unlock, error);
    }

    private static void ObserveScene(SceneShield shield)
    {
        var center = NSNotificationCenter.DefaultCenter;
        // UIKit posts UIScene lifecycle notifications on the main thread. Keep the
        // WillDeactivate cover synchronous so it is visible before the app-switcher snapshot.
        shield.Observers.Add(center.AddObserver(UIScene.WillDeactivateNotification,
            _ => shield.Owner.OnSceneWillDeactivate(shield), shield.Scene));
        shield.Observers.Add(center.AddObserver(UIScene.DidEnterBackgroundNotification,
            _ => shield.Owner.OnSceneBackgrounded(shield), shield.Scene));
        shield.Observers.Add(center.AddObserver(UIScene.DidActivateNotification,
            _ => shield.Owner.OnSceneActivated(shield), shield.Scene));
        shield.Observers.Add(center.AddObserver(UIScene.DidDisconnectNotification,
            _ => shield.Owner.DetachScene(shield), shield.Scene));
    }

    private async void OnSceneBackgrounded(SceneShield shield)
    {
        shield.TransitionGeneration++;
        shield.IsActive = false;
        shield.IsInForeground = false;
        ShowShield(shield, locked: !_coordinator.CanAccessContent);
        try
        {
            await _sceneRegistry.SetInForegroundAsync(shield.Scene, isInForeground: false, GetUnlockReason());
        }
        catch (Exception)
        {
            shield.ErrorMessage = _localization.GetString(ResourceKeys.AppLock_UpdateError);
            shield.ErrorLabel.Text = shield.ErrorMessage;
            ShowShield(shield, locked: true);
        }
    }

    private async void OnSceneActivated(SceneShield shield)
    {
        var generation = ++shield.TransitionGeneration;
        try
        {
            await _sceneRegistry.SetInForegroundAsync(shield.Scene, isInForeground: true, GetUnlockReason());
        }
        catch (Exception)
        {
            shield.ErrorMessage = _localization.GetString(ResourceKeys.AppLock_UpdateError);
            shield.ErrorLabel.Text = shield.ErrorMessage;
            ShowShield(shield, locked: true);
            return;
        }
        if (generation != shield.TransitionGeneration
            || !_shields.TryGetValue(shield.Scene, out var current) || !ReferenceEquals(current, shield)
            || shield.Scene.ActivationState != UISceneActivationState.ForegroundActive)
            return;
        shield.IsInForeground = true;
        shield.IsActive = true;
        RefreshShield(shield.Scene);
    }

    private void OnSceneWillDeactivate(SceneShield shield)
    {
        shield.TransitionGeneration++;
        shield.IsActive = false;
        ShowShield(shield, locked: _coordinator.State == AppLockState.Locked);
    }

    private void DetachScene(SceneShield shield)
    {
        foreach (var window in _scenesByWindow.Where(pair => pair.Value.Handle == shield.Scene.Handle).Select(pair => pair.Key).ToArray())
            _scenesByWindow.Remove(window);
        if (_shields.Remove(shield.Scene))
            DisposeShield(shield);
        _sceneRegistry.Unregister(shield.Scene);
    }

    private void OnCoordinatorStateChanged()
        => MainThread.BeginInvokeOnMainThread(() =>
        {
            foreach (var scene in _shields.Keys.ToArray())
                RefreshShield(scene);
        });

    private void RefreshShield(UIWindowScene scene)
    {
        if (!_shields.TryGetValue(scene, out var shield))
            return;

        var attemptMessage = _coordinator.LatestAuthenticationResult switch
        {
            AuthenticationResult.Unavailable => _localization.GetString(ResourceKeys.AppLock_UnlockUnavailable),
            AuthenticationResult.Failed => _localization.GetString(ResourceKeys.AppLock_AuthenticationFailed),
            _ => null
        };
        if (shield.ErrorMessage is not null)
        {
            shield.ErrorLabel.Text = shield.ErrorMessage;
            shield.ErrorLabel.Hidden = false;
            ShowShield(shield, locked: true);
        }
        else if (shield.IsActive && shield.IsInForeground && _coordinator.CanAccessContent)
        {
            shield.ShieldWindow.Hidden = true;
            shield.HostWindow.MakeKeyWindow();
        }
        else
        {
            ShowShield(shield, locked: _coordinator.State == AppLockState.Locked);
            if (attemptMessage is not null && _coordinator.State == AppLockState.Locked)
            {
                shield.ErrorLabel.Text = attemptMessage;
                shield.ErrorLabel.Hidden = false;
                shield.UnlockButton.SetTitle(GetRetryLabel(), UIControlState.Normal);
            }
        }
    }

    private void ShowShield(SceneShield shield, bool locked)
    {
        shield.ShieldWindow.Hidden = false;
        shield.ShieldWindow.MakeKeyAndVisible();
        shield.View.AccessibilityLabel = locked ? GetUnlockLabel() : "Finanzübersicht";
        shield.UnlockButton.SetTitle(
            shield.ErrorMessage is null ? GetUnlockLabel() : GetRetryLabel(),
            UIControlState.Normal);
        shield.UnlockButton.Hidden = !locked;
        shield.UnlockButton.Enabled = _coordinator.State != AppLockState.Authenticating;
        shield.ErrorLabel.Hidden = shield.ErrorMessage is null;
    }

    private async void OnUnlockTapped(object? sender, EventArgs e)
    {
        var shield = _shields.Values.FirstOrDefault(candidate => ReferenceEquals(candidate.UnlockButton, sender));
        if (shield is null)
            return;

        try
        {
            if (shield.ErrorMessage is not null)
            {
                var window = _scenesByWindow.FirstOrDefault(pair => pair.Value.Handle == shield.Scene.Handle).Key;
                if (window is not null)
                {
                    shield.ErrorMessage = null;
                    shield.ErrorLabel.Text = null;
                    await AttachAsync(window, shield.Scene);
                }
            }
            else
            {
                await _coordinator.UnlockAsync(GetUnlockReason());
            }
        }
        catch (Exception)
        {
            // The coordinator fails closed; keep the shield visible if an adapter throws.
        }
        finally
        {
            foreach (var scene in _shields.Keys.ToArray())
                RefreshShield(scene);
        }
    }

    private static void DisposeShield(SceneShield shield)
    {
        foreach (var observer in shield.Observers)
        {
            NSNotificationCenter.DefaultCenter.RemoveObserver(observer);
            observer.Dispose();
        }
        shield.ShieldWindow.Hidden = true;
        shield.ShieldWindow.RootViewController?.Dispose();
        shield.ShieldWindow.Dispose();
    }

    private string GetUnlockReason()
        => _localization.GetString(ResourceKeys.AppLock_UnlockReason);

    private string GetUnlockLabel()
        => _localization.GetString(ResourceKeys.AppLock_UnlockLabel);

    private string GetRetryLabel()
        => _localization.GetString(ResourceKeys.AppLock_RetryLabel);

    private sealed class SceneShield(
        AppleAppLockBridge owner,
        UIWindowScene scene,
        UIWindow hostWindow,
        UIWindow shieldWindow,
        UIView view,
        UIButton unlockButton,
        UILabel errorLabel)
    {
        public AppleAppLockBridge Owner { get; } = owner;
        public UIWindowScene Scene { get; } = scene;
        public UIWindow HostWindow { get; } = hostWindow;
        public UIWindow ShieldWindow { get; } = shieldWindow;
        public UIView View { get; } = view;
        public UIButton UnlockButton { get; } = unlockButton;
        public UILabel ErrorLabel { get; } = errorLabel;
        public List<NSObject> Observers { get; } = [];
        public string? ErrorMessage { get; set; }
        public long TransitionGeneration { get; set; }
        public bool IsInForeground { get; set; }
        public bool IsActive { get; set; }
    }
}
#endif
