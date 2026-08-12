using System.Windows.Input;
using CommunityToolkit.Maui.Extensions;
using CommunityToolkit.Maui.Views;
using Finanzuebersicht.Controls;
using Finanzuebersicht.Converters;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Microsoft.Maui.Controls.Shapes;

namespace Finanzuebersicht.Views.Popups;

public enum FormSheetResult
{
    Cancelled,
    Saved
}

public class FormSheetPopup : Popup<FormSheetResult>
{
    private const double MaxSheetWidth = 360;
    private const double MinSheetWidth = 280;
    private const double HorizontalMargin = 40;
    private const double ChromeHeight = 120;
    private const double MaxHeightFraction = 0.7;

    private bool _isClosing;
    private bool _isSaving;

    public FormSheetPopup(
        Page hostPage,
        string title,
        View formContent,
        string cancelText,
        string saveText,
        Func<Task<bool>>? trySaveAsync = null)
    {
        BackgroundColor = Colors.Transparent;
        Padding = 0;
        Margin = new Thickness(20);
        CanBeDismissedByTappingOutsideOfPopup = true;

        var (sheetWidth, maxFormHeight) = ComputeSheetSize(hostPage);
        var sheetDescription = string.Format(
            LocalizationResourceManager.Current[ResourceKeys.A11y_FormSheetDialog],
            title);

        var card = new CreateFormCard
        {
            Title = title,
            CancelText = cancelText,
            SaveText = saveText,
            ScrollFormContent = true,
            MaxFormHeight = maxFormHeight,
            AccessibilityDescription = sheetDescription,
            CancelCommand = new Command(() => _ = CloseWithResultAsync(FormSheetResult.Cancelled)),
            SaveCommand = new Command(async () => await OnSaveAsync(trySaveAsync))
        };

        // Defer FormContent until the card is in the visual tree — synchronous assignment
        // during popup construction deadlocks on Mac Catalyst / UIScene.
        void AssignFormContent()
        {
            if (card.FormContent is null)
                card.FormContent = formContent;
        }

        card.Loaded += (_, _) => AssignFormContent();
        card.HandlerChanged += (_, _) =>
        {
            if (card.Handler is not null)
                AssignFormContent();
        };

        var cardBackground = ColorResourceHelper.GetThemeColor(
            "CardBackground", "CardBackgroundDark",
            Color.FromArgb("#FFFFFF"), Color.FromArgb("#1C1C1E"));

        Content = new Border
        {
            Padding = 0,
            BackgroundColor = cardBackground,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 12 },
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Offset = new Point(0, 6),
                Radius = 16,
                Opacity = 0.28f
            },
            Content = new Grid
            {
                WidthRequest = sheetWidth,
                MaximumHeightRequest = maxFormHeight + ChromeHeight,
                Children = { card }
            }
        };

        Microsoft.Maui.Controls.Application.Current?.Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(200),
            () => FormFocusHelper.TryFocusFirstInput(formContent));
    }

    private async Task OnSaveAsync(Func<Task<bool>>? trySaveAsync)
    {
        if (_isClosing || _isSaving)
            return;

        if (trySaveAsync is not null)
        {
            _isSaving = true;
            try
            {
                if (!await trySaveAsync())
                    return;
            }
            finally
            {
                _isSaving = false;
            }
        }

        await CloseWithResultAsync(FormSheetResult.Saved);
    }

    private async Task CloseWithResultAsync(FormSheetResult result)
    {
        if (_isClosing)
            return;

        _isClosing = true;
        await CloseAsync(result);
    }

    private static (double SheetWidth, double MaxFormHeight) ComputeSheetSize(Page hostPage)
    {
        var windowWidth = hostPage.Window?.Width ?? 0;
        var windowHeight = hostPage.Window?.Height ?? 0;

        if (windowWidth <= 0 || windowHeight <= 0)
        {
            var display = DeviceDisplay.MainDisplayInfo;
            windowWidth = display.Width / display.Density;
            windowHeight = display.Height / display.Density;
        }

        var sheetWidth = Math.Min(MaxSheetWidth, Math.Max(MinSheetWidth, windowWidth - HorizontalMargin));
        var maxFormHeight = Math.Max(200, windowHeight * MaxHeightFraction - ChromeHeight);
        return (sheetWidth, maxFormHeight);
    }
}

public static class FormSheetPopupExtensions
{
    public static async Task<bool> ShowFormSheetAsync(
        this Page page,
        string title,
        View formContent,
        Func<Task<bool>> trySaveAsync,
        string? cancelText = null,
        string? saveText = null)
    {
        cancelText ??= LocalizationResourceManager.Current[ResourceKeys.Btn_Abbrechen];
        saveText ??= LocalizationResourceManager.Current[ResourceKeys.Btn_Speichern];

        var popup = new FormSheetPopup(page, title, formContent, cancelText, saveText, trySaveAsync);
        var popupResult = await page.ShowPopupAsync<FormSheetResult>(popup);

        if (popupResult.WasDismissedByTappingOutsideOfPopup)
            return false;

        return popupResult.Result == FormSheetResult.Saved;
    }
}
