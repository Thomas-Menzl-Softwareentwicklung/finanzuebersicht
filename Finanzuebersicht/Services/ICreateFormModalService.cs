namespace Finanzuebersicht.Services;

/// <summary>
/// Stable create-form host (modal ContentPage). Avoids CommunityToolkit Popup / FormSheetPopup.
/// </summary>
public interface ICreateFormModalService
{
    Task<bool> ShowAsync(
        string title,
        Func<View> formContentFactory,
        Func<Task<bool>> trySaveAsync,
        string? cancelText = null,
        string? saveText = null);
}
