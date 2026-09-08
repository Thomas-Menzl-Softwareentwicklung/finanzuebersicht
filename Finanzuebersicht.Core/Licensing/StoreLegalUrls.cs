namespace Finanzuebersicht.Core.Licensing;

/// <summary>
/// Public legal URLs required for auto-renewable subscriptions (Guideline 3.1.2).
/// Apple's standard EULA must appear in the App Store description when no custom EULA is set.
/// </summary>
public static class StoreLegalUrls
{
    public const string AppleStandardEula =
        "https://www.apple.com/legal/internet-services/itunes/dev/stdeula/";

    public const string PrivacyPolicyDe = "https://finanzuebersicht.thomasmenzl.de/privacy.html";
    public const string PrivacyPolicyEn = "https://finanzuebersicht.thomasmenzl.de/en/privacy.html";
}
