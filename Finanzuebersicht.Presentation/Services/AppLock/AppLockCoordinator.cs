using Finanzuebersicht.Core.AppLock;

namespace Finanzuebersicht.Presentation.Services.AppLock;

public class AppLockCoordinator : IAppLockCoordinator
{
    private readonly IAppLockSettingsStore _settings;
    private readonly IDeviceAuthenticationService _authentication;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
    private AppLockOptions _options = AppLockOptions.Default;
    private AppLockState _state = AppLockState.Locked;
    private long _backgroundTimestamp;
    private bool _enteredBackground;
    private bool _initialized;
    private long _generation;
    private readonly object _authenticationSync = new();
    private CancellationTokenSource? _activeAuthentication;

    public AppLockCoordinator(IAppLockSettingsStore settings, IDeviceAuthenticationService authentication, TimeProvider timeProvider)
    {
        _settings = settings;
        _authentication = authentication;
        _timeProvider = timeProvider;
        if (!_authentication.IsSupported)
        {
            _initialized = true;
            _state = AppLockState.Disabled;
        }
    }

    public AppLockState State => _state;
    public AuthenticationResult? LatestAuthenticationResult { get; private set; }
    public bool CanAccessContent => _state is AppLockState.Disabled or AppLockState.Unlocked;
    public event Action? StateChanged;

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (_initialized)
                return;
            SetState(AppLockState.Locked);
            var loaded = await _settings.LoadAsync(cancellationToken);
            var delay = AppLockOptions.AllowedDelaysSeconds.Contains(loaded.DelaySeconds)
                ? loaded.DelaySeconds
                : 0;
            _options = new AppLockOptions(loaded.Enabled, delay);
            _initialized = true;
            _enteredBackground = false;
            SetState(_options.Enabled ? AppLockState.Locked : AppLockState.Disabled);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public void EnterBackground()
    {
        lock (_authenticationSync)
        {
            _generation++;
            _activeAuthentication?.Cancel();
        }
        _backgroundTimestamp = _timeProvider.GetTimestamp();
        _enteredBackground = true;
    }

    public async Task ActivateAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            return;

        var generation = _generation;
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (generation != _generation || !_enteredBackground)
                return;

            var elapsed = _timeProvider.GetElapsedTime(_backgroundTimestamp);
            _enteredBackground = false;
            if (_options.Enabled && elapsed >= TimeSpan.FromSeconds(_options.DelaySeconds))
                SetState(AppLockState.Locked);

            if (_state == AppLockState.Locked)
                await AuthenticateAndUpdateStateAsync(reason, cancellationToken, generation);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task UnlockAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (!_initialized || _state != AppLockState.Locked)
            return;

        var generation = _generation;
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (generation == _generation && _state == AppLockState.Locked)
                await AuthenticateAndUpdateStateAsync(reason, cancellationToken, generation);
        }
        finally
        {
            _operationGate.Release();
        }
    }

    public async Task<bool> ChangeOptionsAsync(AppLockOptions value, string reason, CancellationToken cancellationToken = default)
    {
        value.Validate();
        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (!_initialized)
                return false;

            if (!_authentication.IsSupported || _state is AppLockState.Locked or AppLockState.Authenticating)
                return false;

            var requiresAuthentication = value.Enabled != _options.Enabled
                || (value.Enabled && value.DelaySeconds > _options.DelaySeconds);
            if (requiresAuthentication)
            {
                if (!await AuthenticateAsync(reason, cancellationToken, _generation))
                    return false;
            }

            var generation = _generation;
            var previousState = _state;
            await _settings.SaveAsync(value, cancellationToken);
            _options = value;
            if (generation == _generation)
                _enteredBackground = false;
            SetState(generation == _generation
                ? requiresAuthentication ? value.Enabled ? AppLockState.Unlocked : AppLockState.Disabled : previousState
                : value.Enabled ? AppLockState.Locked : AppLockState.Disabled);
            return true;
        }
        finally
        {
            _operationGate.Release();
        }
    }

    private async Task AuthenticateAndUpdateStateAsync(string reason, CancellationToken cancellationToken, long generation)
    {
        LatestAuthenticationResult = null;
        SetState(AppLockState.Authenticating);
        var succeeded = await AuthenticateAsync(reason, cancellationToken, generation);
        SetState(succeeded && generation == _generation ? AppLockState.Unlocked : AppLockState.Locked);
    }

    private async Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken, long generation)
    {
        using var attempt = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        lock (_authenticationSync)
        {
            if (generation != _generation)
                return false;
            _activeAuthentication = attempt;
        }
        AuthenticationResult result;
        try
        {
            var available = await _authentication.IsAvailableAsync(attempt.Token);
            if (attempt.IsCancellationRequested || generation != _generation)
                result = AuthenticationResult.Cancelled;
            else
                result = available
                    ? await _authentication.AuthenticateAsync(reason, attempt.Token)
                    : AuthenticationResult.Unavailable;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (OperationCanceledException) when (attempt.IsCancellationRequested)
        {
            result = AuthenticationResult.Cancelled;
        }
        catch (Exception)
        {
            result = AuthenticationResult.Failed;
        }
        finally
        {
            lock (_authenticationSync)
            {
                if (ReferenceEquals(_activeAuthentication, attempt))
                    _activeAuthentication = null;
            }
        }
        if (generation != _generation)
            return false;
        LatestAuthenticationResult = result;
        return result == AuthenticationResult.Succeeded;
    }

    private void SetState(AppLockState value)
    {
        if (_state == value)
            return;
        _state = value;
        StateChanged?.Invoke();
    }
}
