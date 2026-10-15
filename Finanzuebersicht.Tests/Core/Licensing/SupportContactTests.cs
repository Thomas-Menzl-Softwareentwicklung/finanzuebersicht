using Finanzuebersicht.Core.Licensing;

namespace Finanzuebersicht.Tests.Core.Licensing;

public class SupportContactTests
{
    [Fact]
    public void CreateLanguageRequestMailto_EscapesSubject()
    {
        var uri = SupportContact.CreateLanguageRequestMailto("Finanzübersicht Sprache");

        Assert.Equal(
            "mailto:info@thomasmenzl.de?subject=Finanz%C3%BCbersicht%20Sprache",
            uri);
    }
}
