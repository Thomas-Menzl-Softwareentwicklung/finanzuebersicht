#if IOS || MACCATALYST
using System.Runtime.Versioning;
using CoreGraphics;
using Finanzuebersicht.Helpers;
using Foundation;
using Microsoft.Maui.Controls.PlatformConfiguration;
using Microsoft.Maui.Platform;
using ObjCRuntime;
using UIKit;
using MauiIosPage = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.Page;
using MauiModalStyle = Microsoft.Maui.Controls.PlatformConfiguration.iOSSpecific.UIModalPresentationStyle;

namespace Finanzuebersicht.Services;

/// <summary>
/// Presents a MAUI <see cref="Microsoft.Maui.Controls.Page"/> as an iOS bottom sheet
/// or a Mac Catalyst centered form dialog (#353).
/// </summary>
internal static class CreateFormSheetPresentation
{
    const string FitDetentId = "finanz.create.fit";
    const double MacDialogWidth = 480;
    const double MacDialogMinHeight = 420;

    public static void PreferPageSheet(Microsoft.Maui.Controls.Page page)
    {
        // Must be set before PushModalAsync so UIKit creates a sheet/dialog, not fullscreen.
        if (OperatingSystem.IsMacCatalyst())
        {
            MauiIosPage.SetModalPresentationStyle(page.On<iOS>(), MauiModalStyle.FormSheet);
            return;
        }

        MauiIosPage.SetModalPresentationStyle(page.On<iOS>(), MauiModalStyle.PageSheet);
    }

    public static void AttachFittingDetents(Microsoft.Maui.Controls.Page page, View measureRoot)
    {
        // Mac: FormSheet is a centered dialog — no grabber/detents (#353).
        if (OperatingSystem.IsMacCatalyst())
        {
            AttachMacDialogSize(page, measureRoot);
            AttachMacEscapeDismiss(page);
            return;
        }

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

    static void AttachMacDialogSize(Microsoft.Maui.Controls.Page page, View measureRoot)
    {
        void Apply()
        {
            if (page.Handler is not IPlatformViewHandler { ViewController: { } vc })
                return;

            var width = MacDialogWidth;
            var measured = measureRoot.Measure(width, double.PositiveInfinity);
            var height = measured.Height;
            if (height <= 0 || double.IsNaN(height))
                height = MacDialogMinHeight;
            height = Math.Clamp(height + 24, MacDialogMinHeight, 720);

            vc.PreferredContentSize = new CGSize(width, height);
        }

        page.HandlerChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
        measureRoot.SizeChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
        page.Loaded += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
    }

    /// <summary>
    /// FormSheet keeps Esc out of the AppDelegate menu responder path; a focused Entry
    /// would otherwise only resign first responder (focus gone, dialog stays, next Esc beeps).
    /// Child VC key commands participate in the modal hierarchy with system priority.
    /// </summary>
    static void AttachMacEscapeDismiss(Microsoft.Maui.Controls.Page page)
    {
        MacEscapeKeyViewController? child = null;

        void Apply()
        {
            if (child is not null)
                return;
            if (page.Handler is not IPlatformViewHandler { ViewController: { } vc } || vc.View is null)
                return;

            child = new MacEscapeKeyViewController(() => DesktopMenuBridge.DismissModal());
            vc.AddChildViewController(child);
            child.View!.Frame = CGRect.Empty;
            child.View.UserInteractionEnabled = false;
            vc.View.AddSubview(child.View);
            child.DidMoveToParentViewController(vc);
        }

        page.HandlerChanged += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
        page.Loaded += (_, _) => MainThread.BeginInvokeOnMainThread(Apply);
    }

    sealed class MacEscapeKeyViewController : UIViewController
    {
        readonly Action _dismiss;

        public MacEscapeKeyViewController(Action dismiss) => _dismiss = dismiss;

        public override void ViewDidLoad()
        {
            base.ViewDidLoad();
            var escape = UIKeyCommand.Create(
                (NSString)UIKeyCommand.Escape,
                default,
                new Selector("onFinanzMacEscape:"));
            escape.WantsPriorityOverSystemBehavior = true;
            AddKeyCommand(escape);
        }

        [Export("onFinanzMacEscape:")]
        void OnFinanzMacEscape(UIKeyCommand command) => _dismiss();
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
