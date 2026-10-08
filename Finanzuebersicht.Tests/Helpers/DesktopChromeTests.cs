using Finanzuebersicht.Core.Platform;

namespace Finanzuebersicht.Tests.Helpers;

public class DesktopChromeTests
{
    [Fact]
    public void IsPhone_IsNegationOfIsDesktop()
    {
        Assert.Equal(!DesktopChrome.IsDesktop, DesktopChrome.IsPhone);
    }

    [Fact]
    public void ContentMaxWidth_IsWideDesktopSoftCap()
    {
        Assert.Equal(1200, DesktopChrome.ContentMaxWidth);
    }

    [Fact]
    public void DetailOpenTapCount_IsDoubleOnDesktopOtherwiseSingle()
    {
        var expected = DesktopChrome.IsDesktop ? 2 : 1;
        Assert.Equal(expected, DesktopChrome.DetailOpenTapCount);
    }
}
