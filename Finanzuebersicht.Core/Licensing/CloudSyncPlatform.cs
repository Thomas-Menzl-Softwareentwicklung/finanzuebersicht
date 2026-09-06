namespace Finanzuebersicht.Core.Licensing;

public static class CloudSyncPlatform
{
    public static bool IsFeatureAvailable() =>
        OperatingSystem.IsIOSVersionAtLeast(17)
        || OperatingSystem.IsMacCatalystVersionAtLeast(17);
}
