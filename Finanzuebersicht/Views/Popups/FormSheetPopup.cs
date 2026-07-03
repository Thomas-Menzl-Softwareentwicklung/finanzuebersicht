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
    private const double SheetWidth = 360;
    private const double ChromeHeight = 120;
    private const double MaxHeightFraction = 0.7;

    private bool _isClosing;

    public FormSheetPopup(string title, View formContent, string cancelText, string saveText)
    {
        BackgroundColor = Colors.Transparent;
        Padding = 0;
        Margin = new Thickness(20);
        CanBeDismissedByTappingOutsideOfPopup = true;

        var maxFormHeight = ComputeMaxFormHeight();
        var sheetDescription = string.Format(
            LocalizationResourceManager.Current[ResourceKeys.A11y_FormSheetDialog],
            title);

        var card = new CreateFormCard
        {
            Title = title,
            FormContent = formContent,
            CancelText = cancelText,
            SaveText = saveText,
            ScrollFormContent = true,
            MaxFormHeight = maxFormHeight,
            AccessibilityDescription = sheetDescription,
            CancelCommand = new Command(() => _ = CloseWithResultAsync(FormSheetResult.Cancelled)),
            SaveCommand = new Command(() => _ = CloseWithResultAsync(FormSheetResult.Saved))
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
                WidthRequest = SheetWidth,
                MaximumHeightRequest = maxFormHeight + ChromeHeight,
                Children = { card }
            }
        };

        Microsoft.Maui.Controls.Application.Current?.Dispatcher.DispatchDelayed(
            TimeSpan.FromMilliseconds(200),
            () => FormFocusHelper.TryFocusFirstInput(formContent));
    }

    private async Task CloseWithResultAsync(FormSheetResult result)
    {
        if (_isClosing)
            return;

        _isClosing = true;
        await CloseAsync(result);
    }

    private static double ComputeMaxFormHeight()
    {
        var display = DeviceDisplay.MainDisplayInfo;
        var heightDp = display.Height / display.Density;
        return Math.Max(200, heightDp * MaxHeightFraction - ChromeHeight);
    }
}

public static class FormSheetPopupExtensions
{
    public static async Task<FormSheetResult> ShowFormSheetAsync(
        this Page page,
        string title,
        View formContent,
        string? cancelText = null,
        string? saveText = null)
    {
        cancelText ??= LocalizationResourceManager.Current[ResourceKeys.Btn_Abbrechen];
        saveText ??= LocalizationResourceManager.Current[ResourceKeys.Btn_Speichern];

        var popup = new FormSheetPopup(title, formContent, cancelText, saveText);
        var popupResult = await page.ShowPopupAsync<FormSheetResult>(popup);

        if (popupResult.WasDismissedByTappingOutsideOfPopup)
            return FormSheetResult.Cancelled;

        return popupResult.Result;
    }
}
