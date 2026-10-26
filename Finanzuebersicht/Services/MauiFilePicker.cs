using System.Globalization;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Core.Services.ScreenshotDemo;
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
#if DEBUG && !APP_DISTRIBUTION_STORE
        // Gate here too: the Core assembly does not receive the MAUI Store build symbol.
        if (ScreenshotDemoLaunchOptions.IsActive())
        {
            var bytes = ScreenshotDemoCsv.Create(SystemClock.Instance, CultureInfo.CurrentUICulture);
            return new PickFileResult("demo-transactions.csv",
                () => Task.FromResult<Stream>(new MemoryStream(bytes, writable: false)));
        }

#endif
        var result = await FilePicker.Default.PickAsync(CsvPickOptions);
        if (result is null) return null;
        return new PickFileResult(result.FileName, () => result.OpenReadAsync());
    }
}
