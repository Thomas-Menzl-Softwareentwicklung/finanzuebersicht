#if IOS || MACCATALYST
using System.Runtime.Versioning;
using Foundation;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using UIKit;
using MauiIosPage = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page;
using MauiModalStyle = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle;

namespace Finanzuebersicht.Services;

/// <summary>
/// Presents a MAUI <see cref="Microsoft.Maui.Controls.Page"/> as an iOS/Mac Catalyst bottom sheet
/// sized to content when possible.
/// </summary>
internal static class CreateFormSheetPresentation
{
    const string FitDetentId = "finanz.create.fit";

    public static void PreferPageSheet(Microsoft.Maui.Controls.Page page)
    {
        // Must be set before PushModalAsync so UIKit creates a sheet, not fullscreen.
        MauiIosPage.SetModalPresentationStyle(page.On<iOS>(), MauiModalStyle.PageSheet);
    }

    public static void AttachFittingDetents(Microsoft.Maui.Controls.Page page, View measureRoot)
    {
        void Apply()
        {
            if (page.Handler is not IPlatformViewHandler { ViewController: { } vc })
                return;

            var sheet = vc.SheetPresentationController;
            if (sheet is null)
                return;

            sheet.PrefersGrabberVisible = true;
            sheet.PrefersScrollingExpandsWhenScrolledToEdge = false;
            sheet.PreferredCornerRadius = 14;

            if (OperatingSystem.IsIOSVersionAtLeast(16) || OperatingSystem.IsMacCatalystVersionAtLeast(16))
            {
                ApplyFittingDetents(sheet, page, measureRoot);
            }
            else
            {
                sheet.Detents =
                [
                    UISheetPresentationControllerDetent.CreateMediumDetent(),
                    UISheetPresentationControllerDetent.CreateLargeDetent()
                ];
                sheet.SelectedDetentIdentifier = UISheetPresentationControllerDetentIdentifier.Medium;
            }
        }

        page.HandlerChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
        measureRoot.SizeChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
        page.Loaded += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
    }

    [SupportedOSPlatform("ios16.0")]
    [SupportedOSPlatform("maccatalyst16.0")]
    static void ApplyFittingDetents(
        UISheetPresentationController sheet,
        Microsoft.Maui.Controls.Page page,
        View measureRoot)
    {
        nfloat Resolve(IUISheetPresentationControllerDetentResolutionContext context)
        {
            var width = page.Width;
            if (width <= 0 || double.IsNaN(width))
                width = 390;

            var measured = measureRoot.Measure(width, double.PositiveInfinity);
            var height = measured.Height;
            if (height <= 0 || double.IsNaN(height))
                height = (double)context.MaximumDetentValue * 0.55;

            // Grabber + safe breathing room above home indicator.
            height += 28;
            return (nfloat)Math.Min(height, (double)context.MaximumDetentValue);
        }

        var fit = UISheetPresentationControllerDetent.Create(FitDetentId, Resolve);
        sheet.Detents =
        [
            fit,
            UISheetPresentationControllerDetent.CreateLargeDetent()
        ];
        sheet.SelectedDetentIdentifier =
            UISheetPresentationControllerDetentIdentifierExtensions.GetValue(new NSString(FitDetentId));
        sheet.InvalidateDetents();
    }
}
#endif
