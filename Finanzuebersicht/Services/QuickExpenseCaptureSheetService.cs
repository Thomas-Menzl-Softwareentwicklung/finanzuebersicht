using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Services;

/// <summary>
/// Minimal modal quick-expense UI. Intentionally avoids CreateFormCard, FormSheet,
/// CommunityToolkit Popup, and NavigationPage — all of which have crashed on iOS/Mac.
/// </summary>
public sealed class QuickExpenseCaptureSheetService : IQuickExpenseCaptureSheetService
{
    public async Task<bool> ShowAsync(QuickExpenseCaptureViewModel viewModel)
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
        var amountEntry = new Entry
        {
            Placeholder = loc[ResourceKeys.Hint_Betrag],
            Keyboard = Keyboard.Numeric,
            FontSize = 28,
            FontAttributes = FontAttributes.Bold,
            HorizontalTextAlignment = TextAlignment.Center
        };
        amountEntry.SetBinding(Entry.TextProperty, nameof(QuickExpenseCaptureViewModel.BetragText));

        var titleEntry = new Entry
        {
            Placeholder = loc[ResourceKeys.Hint_SchnellAusgabeInfo],
            FontSize = 16
        };
        titleEntry.SetBinding(Entry.TextProperty, nameof(QuickExpenseCaptureViewModel.Titel));

        var cancelButton = new Button
        {
            Text = loc[ResourceKeys.Btn_Abbrechen],
            Command = new Command(async () =>
            {
                Complete(false);
                if (navigation.ModalStack.Count > 0)
                    await navigation.PopModalAsync();
            })
        };

        var saveButton = new Button
        {
            Text = loc[ResourceKeys.Btn_Speichern],
            Command = new Command(async () =>
            {
                try
                {
                    if (!await viewModel.TrySaveAsync())
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

        var page = new ContentPage
        {
            Title = viewModel.PageTitle,
            BindingContext = viewModel,
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
                            Text = loc[ResourceKeys.Lbl_SchnellAusgabeHinweis],
                            FontSize = 14
                        },
                        new Label { Text = loc[ResourceKeys.Lbl_Betrag], FontSize = 13 },
                        amountEntry,
                        new Label { Text = loc[ResourceKeys.Lbl_Info], FontSize = 13 },
                        titleEntry,
                        new Grid
                        {
                            ColumnDefinitions = new ColumnDefinitionCollection
                            {
                                new(GridLength.Star),
                                new(GridLength.Star)
                            },
                            ColumnSpacing = 10,
                            Children = { cancelButton, saveButton }
                        }
                    }
                }
            }
        };
        Grid.SetColumn(saveButton, 1);

        page.Disappearing += (_, _) => Complete(false);

        try
        {
            // Plain ContentPage modal — no NavigationPage wrapper.
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
