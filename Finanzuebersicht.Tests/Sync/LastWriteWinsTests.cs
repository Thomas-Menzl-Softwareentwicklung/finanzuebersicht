using Finanzuebersicht.Core.Sync;

namespace Finanzuebersicht.Tests.Sync;

public class LastWriteWinsTests
{
    [Theory]
    [InlineData(null, "2026-01-02", true)]
    [InlineData("2026-01-02", null, false)]
    [InlineData("2026-01-01", "2026-01-02", true)]
    [InlineData("2026-01-02", "2026-01-01", false)]
    [InlineData("2026-01-02", "2026-01-02", true)]
    [InlineData(null, null, true)]
    public void RemoteWins_Matrix(string? local, string? remote, bool expectRemote)
    {
        DateTime? L = local is null ? null : DateTime.Parse(local, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        DateTime? R = remote is null ? null : DateTime.Parse(remote, null, System.Globalization.DateTimeStyles.AssumeUniversal | System.Globalization.DateTimeStyles.AdjustToUniversal);
        Assert.Equal(expectRemote, LastWriteWins.RemoteWins(L, R));
    }
}
