#if IOS || MACCATALYST
using Finanzuebersicht.Core.AppLock;
using Foundation;
using LocalAuthentication;

namespace Finanzuebersicht.Services.AppLock;

public sealed class AppleDeviceAuthenticationService : IDeviceAuthenticationService
{
    public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new LAContext();
        var available = context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out _);
        context.Invalidate();
        return Task.FromResult(available);
    }

    public async Task<AuthenticationResult> AuthenticateAsync(string reason, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        using var context = new LAContext();
        if (!context.CanEvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, out _))
            return AuthenticationResult.Unavailable;

        var completion = new TaskCompletionSource<(bool Success, NSError? Error)>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = cancellationToken.Register(context.Invalidate);
        try
        {
            context.EvaluatePolicy(LAPolicy.DeviceOwnerAuthentication, reason,
                (success, error) => completion.TrySetResult((success, error)));
            var (success, error) = await completion.Task;
            if (success)
                return AuthenticationResult.Succeeded;
            if (error?.Code == (long)LAStatus.UserCancel || error?.Code == (long)LAStatus.AppCancel)
                return AuthenticationResult.Cancelled;
            return AuthenticationResult.Failed;
        }
        catch (OperationCanceledException)
        {
            return AuthenticationResult.Cancelled;
        }
        finally
        {
            context.Invalidate();
        }
    }
}
#endif
