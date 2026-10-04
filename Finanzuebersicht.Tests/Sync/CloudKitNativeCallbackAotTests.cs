namespace Finanzuebersicht.Tests.Sync;

/// <summary>
/// iOS AOT cannot JIT reverse P/Invoke stubs. The CloudKit records callback must be
/// <c>UnmanagedCallersOnly</c> (or <c>MonoPInvokeCallback</c>), otherwise Store/TestFlight
/// builds abort in <c>mono_jit_exec</c> during CloudKitSyncTransport construction.
/// </summary>
public class CloudKitNativeCallbackAotTests
{
    [Fact]
    public void HandleNativeRecords_IsUnmanagedCallersOnly()
    {
        var source = File.ReadAllText(FindTransportSource());

        Assert.Contains("[UnmanagedCallersOnly", source, StringComparison.Ordinal);
        Assert.Contains("HandleNativeRecords", source, StringComparison.Ordinal);
        Assert.Contains("delegate* unmanaged[Cdecl]<IntPtr, void>", source, StringComparison.Ordinal);
        Assert.DoesNotContain("UnmanagedFunctionPointer", source, StringComparison.Ordinal);
        Assert.DoesNotContain("RootedRecordsCallback", source, StringComparison.Ordinal);
    }

    private static string FindTransportSource()
    {
        var dir = new DirectoryInfo(AppContext.BaseDirectory);
        while (dir is not null)
        {
            var candidate = Path.Combine(
                dir.FullName,
                "Finanzuebersicht",
                "CloudKit",
                "CloudKitSyncTransport.cs");
            if (File.Exists(candidate))
            {
                return candidate;
            }

            dir = dir.Parent;
        }

        throw new InvalidOperationException("CloudKitSyncTransport.cs not found from test output directory.");
    }
}
