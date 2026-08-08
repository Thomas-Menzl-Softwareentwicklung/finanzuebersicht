using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.Services;

/// <summary>
/// Modal create-form host modeled on <see cref="QuickExpenseCaptureSheetService"/> —
/// plain ContentPage, no Toolkit Popup, no NavigationPage wrapper.
/// </summary>
public sealed class CreateFormModalService : ICreateFormModalService
{
    public async Task<bool> ShowAsync(
        string title,
        Func<View> formContentFactory,
        Func<Task<bool>> trySaveAsync,
        string? cancelText = null,
        string? saveText = null)
    {
        var navigation = Shell.Current?.Navigation;
        if (navigation is null)
            return false;

        var tcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var settled = 0;

        void Complete(bool saved)
        {
            if (Interlocked.Exchange(ref settled, 1) == 0)
                tcs.TrySetResult(saved);
        }

        var loc = LocalizationResourceManager.Current;
        cancelText ??= loc[ResourceKeys.Btn_Abbrechen];
        saveText ??= loc[ResourceKeys.Btn_Speichern];

        var cancelButton = new Button
        {
            Text = cancelText,
            Command = new Command(async () =>
            {
                Complete(false);
                if (navigation.ModalStack.Count > 0)
                    await navigation.PopModalAsync();
            })
        };

        var saveButton = new Button
        {
            Text = saveText,
            Command = new Command(async () =>
            {
                try
                {
                    if (!await trySaveAsync())
                        return;

                    Complete(true);
                    if (navigation.ModalStack.Count > 0)
                        await navigation.PopModalAsync();
                }
                catch (Exception ex)
                {
                    if (Shell.Current is not null)
                    {
                        await Shell.Current.DisplayAlertAsync(
                            loc[ResourceKeys.Err_Titel],
                            ex.Message,
                            loc[ResourceKeys.Btn_OK]);
                    }
                }
            })
        };

        // Defer form construction until the page is in the visual tree — synchronous
        // FormContent assignment during init has deadlocked on Mac Catalyst / UIScene.
        var formHost = new ContentView();
        void AssignForm()
        {
            if (formHost.Content is not null)
                return;
            formHost.Content = formContentFactory();
        }

        formHost.Loaded += (_, _) => AssignForm();
        formHost.HandlerChanged += (_, _) =>
        {
            if (formHost.Handler is not null)
                AssignForm();
        };

        var buttonRow = new Grid
        {
            ColumnDefinitions = new ColumnDefinitionCollection
            {
                new(GridLength.Star),
                new(GridLength.Star)
            },
            ColumnSpacing = 10,
            Children = { cancelButton, saveButton }
        };
        Grid.SetColumn(saveButton, 1);

        var page = new ContentPage
        {
            Title = title,
            Content = new ScrollView
            {
                Content = new VerticalStackLayout
                {
                    Padding = new Thickness(20),
                    Spacing = 16,
                    Children =
                    {
                        new Label
                        {
                            Text = title,
                            FontSize = 20,
                            FontAttributes = FontAttributes.Bold
                        },
                        formHost,
                        buttonRow
                    }
                }
            }
        };

        page.Disappearing += (_, _) => Complete(false);

        try
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
                await navigation.PushModalAsync(page));
        }
        catch (Exception ex)
        {
            Complete(false);
            if (Shell.Current is not null)
            {
                await Shell.Current.DisplayAlertAsync(
                    loc[ResourceKeys.Err_Titel],
                    ex.Message,
                    loc[ResourceKeys.Btn_OK]);
            }

            return false;
        }

        return await tcs.Task;
    }
}
