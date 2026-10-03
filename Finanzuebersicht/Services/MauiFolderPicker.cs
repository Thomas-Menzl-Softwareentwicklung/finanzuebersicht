namespace Finanzuebersicht.Services;

public class MauiFolderPicker : IFolderPicker
{
    public Task<string?> PickAsync()
    {
        if (MainThread.IsMainThread)
            return PickCoreAsync();

        return MainThread.InvokeOnMainThreadAsync(PickCoreAsync);
    }

    private static async Task<string?> PickCoreAsync()
    {
        var result = await CommunityToolkit.Maui.Storage.FolderPicker.Default.PickAsync();
        return result.IsSuccessful ? result.Folder?.Path : null;
    }
}
