using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace Finanzuebersicht.Infrastructure.Services;

/// <summary>Monotonic Apple clock that includes device sleep in app-lock background durations.</summary>
public sealed class AppleContinuousTimeProvider : TimeProvider
{
    // Darwin CLOCK_MONOTONIC_RAW includes sleep; CLOCK_UPTIME_RAW (8), used by
    // Stopwatch/TimeProvider.System, excludes sleep and cannot enforce this timeout.
    private const int ContinuousClockId = 4;
    private readonly Func<int, ulong> _readClock;

    [SupportedOSPlatform("ios10.0")]
    [SupportedOSPlatform("maccatalyst13.1")]
    [SupportedOSPlatform("macos10.12")]
    public AppleContinuousTimeProvider() : this(ReadClock) { }

    internal AppleContinuousTimeProvider(Func<int, ulong> readClock)
        => _readClock = readClock;

    public override long TimestampFrequency => 1_000_000_000;

    public override long GetTimestamp() => checked((long)_readClock(ContinuousClockId));

    [DllImport("/usr/lib/libSystem.dylib", EntryPoint = "clock_gettime_nsec_np")]
    private static extern ulong ReadClock(int clockId);
}
