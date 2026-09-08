using Finanzuebersicht.Presentation.Services;

namespace Finanzuebersicht.Services;

public sealed class MauiExternalBrowser : IExternalBrowser
{
    public Task OpenAsync(Uri uri) => Launcher.Default.OpenAsync(uri);
}
