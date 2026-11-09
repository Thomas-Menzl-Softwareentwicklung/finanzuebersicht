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

    public AppLockCoordinator(IAppLockSettingsStore settings, IDeviceAuthenticationService authentication, TimeProvider timeProvider)
    {
        _settings = settings;
        _authentication = authentication;
        _timeProvider = timeProvider;
    }

    public AppLockState State => _state;
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
        _generation++;
        _backgroundTimestamp = _timeProvider.GetTimestamp();
        _enteredBackground = true;
    }

    public async Task ActivateAsync(string reason, CancellationToken cancellationToken = default)
    {
        if (!_initialized)
            return;

        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (!_enteredBackground)
                return;

            var elapsed = _timeProvider.GetElapsedTime(_backgroundTimestamp);
            _enteredBackground = false;
            if (_options.Enabled && elapsed >= TimeSpan.FromSeconds(_options.DelaySeconds))
                SetState(AppLockState.Locked);

            if (_state == AppLockState.Locked)
                await AuthenticateAndUpdateStateAsync(reason, cancellationToken, _generation);
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

        await _operationGate.WaitAsync(cancellationToken);
        try
        {
            if (_state == AppLockState.Locked)
                await AuthenticateAndUpdateStateAsync(reason, cancellationToken, _generation);
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

            var requiresAuthentication = value.Enabled != _options.Enabled
                || (value.Enabled && value.DelaySeconds > _options.DelaySeconds);
            if (requiresAuthentication)
            {
                if (!await AuthenticateAsync(reason, cancellationToken, _generation))
                    return false;
            }

            var generation = _generation;
            await _settings.SaveAsync(value, cancellationToken);
            _options = value;
            _enteredBackground = false;
            SetState(generation == _generation
                ? value.Enabled ? AppLockState.Unlocked : AppLockState.Disabled
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
        SetState(AppLockState.Authenticating);
        var succeeded = await AuthenticateAsync(reason, cancellationToken, generation);
        SetState(succeeded && generation == _generation ? AppLockState.Unlocked : AppLockState.Locked);
    }

    private async Task<bool> AuthenticateAsync(string reason, CancellationToken cancellationToken, long generation)
    {
        try
        {
            if (!await _authentication.IsAvailableAsync(cancellationToken))
                return false;
            var result = await _authentication.AuthenticateAsync(reason, cancellationToken);
            return result == AuthenticationResult.Succeeded && generation == _generation;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    private void SetState(AppLockState value)
    {
        if (_state == value)
            return;
        _state = value;
        StateChanged?.Invoke();
    }
}
