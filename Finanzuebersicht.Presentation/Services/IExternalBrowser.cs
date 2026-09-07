namespace Finanzuebersicht.Presentation.Services;

/// <summary>
/// Opens https URLs in the system browser so ViewModels stay testable without MAUI.
/// </summary>
public interface IExternalBrowser
{
    Task OpenAsync(Uri uri);
}
