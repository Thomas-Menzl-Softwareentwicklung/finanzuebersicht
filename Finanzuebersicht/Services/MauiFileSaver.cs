namespace Finanzuebersicht.Services;

public class MauiFileSaver : IFileSaver
{
    public Task<FileSaverResult> SaveAsync(string fileName, Stream data, CancellationToken cancellationToken)
    {
        if (MainThread.IsMainThread)
            return SaveCoreAsync(fileName, data, cancellationToken);

        return MainThread.InvokeOnMainThreadAsync(() => SaveCoreAsync(fileName, data, cancellationToken));
    }

    private static async Task<FileSaverResult> SaveCoreAsync(
        string fileName,
        Stream data,
        CancellationToken cancellationToken)
    {
        var result = await CommunityToolkit.Maui.Storage.FileSaver.Default.SaveAsync(
            fileName,
            data,
            cancellationToken);
        return new FileSaverResult(result.IsSuccessful, result.FilePath, result.Exception);
    }
}
