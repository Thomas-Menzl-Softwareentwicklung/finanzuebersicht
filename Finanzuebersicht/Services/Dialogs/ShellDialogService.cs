namespace Finanzuebersicht.Services;

public class ShellDialogService : IDialogService
{
    public async Task ShowAlertAsync(string title, string message, string cancel)
    {
        if (Shell.Current is null) return;

        await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            await Shell.Current.DisplayAlertAsync(title, message, cancel);
        });
    }

    public async Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel)
    {
        if (Shell.Current is null) return false;

        return await MainThread.InvokeOnMainThreadAsync(async () =>
        {
            return await Shell.Current.DisplayAlertAsync(title, message, accept, cancel);
        });
    }

    public async Task<string?> ShowPromptAsync(string title, string message, string accept, string cancel, string initialValue)
    {
        if (Shell.Current is null) return null;

        return await MainThread.InvokeOnMainThreadAsync(async () =>
            await Shell.Current.DisplayPromptAsync(
                title, message, accept, cancel, initialValue: initialValue, keyboard: Keyboard.Numeric));
    }
}
