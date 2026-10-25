namespace Finanzuebersicht.Presentation.Services;

public interface IDialogService
{
    Task ShowAlertAsync(string title, string message, string cancel);
    Task<bool> ShowConfirmationAsync(string title, string message, string accept, string cancel);
    Task<string?> ShowPromptAsync(string title, string message, string accept, string cancel, string initialValue);
}
