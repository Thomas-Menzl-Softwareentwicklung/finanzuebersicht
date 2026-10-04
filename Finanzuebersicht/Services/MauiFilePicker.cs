using Microsoft.Maui.Storage;

namespace Finanzuebersicht.Services;

/// <summary>
/// MAUI implementation: wraps <see cref="FilePicker"/> so ViewModels stay platform-agnostic.
/// Must run on the UI thread (MAUI permission / presentation checks).
/// </summary>
public class MauiFilePicker : Finanzuebersicht.Presentation.Services.IFilePicker
{
    private static readonly PickOptions CsvPickOptions = new()
    {
        PickerTitle = "CSV",
        FileTypes = new FilePickerFileType(new Dictionary<DevicePlatform, IEnumerable<string>>
        {
            // UTIs — bank exports are almost always text/csv or plain text with .csv
            { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.text", "public.data" } },
            { DevicePlatform.MacCatalyst, new[] { "public.comma-separated-values-text", "public.text", "public.data" } },
            { DevicePlatform.Android, new[] { "text/csv", "text/comma-separated-values", "text/plain", "*/*" } },
            { DevicePlatform.WinUI, new[] { ".csv", ".txt" } },
        }),
    };

    public Task<PickFileResult?> PickAsync()
    {
        if (MainThread.IsMainThread)
            return PickCoreAsync();

        return MainThread.InvokeOnMainThreadAsync(PickCoreAsync);
    }

    private static async Task<PickFileResult?> PickCoreAsync()
    {
        var result = await FilePicker.Default.PickAsync(CsvPickOptions);
        if (result is null) return null;
        return new PickFileResult(result.FileName, () => result.OpenReadAsync());
    }
}
