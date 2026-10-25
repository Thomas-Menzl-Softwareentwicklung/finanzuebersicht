namespace Finanzuebersicht.Controls;

/// <summary>
/// Account balances can be negative. <c>Keyboard.Numeric</c> maps to the iOS decimal pad,
/// which has no minus key and, on Mac Catalyst, also drops the hardware hyphen.
/// </summary>
public static class SignedAmountEntry
{
    public static readonly BindableProperty AllowNegativeProperty =
        BindableProperty.CreateAttached(
            "AllowNegative",
            typeof(bool),
            typeof(SignedAmountEntry),
            false,
            propertyChanged: OnAllowNegativeChanged);

#if IOS || MACCATALYST
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<UIKit.UITextField, object> SelectAllHooked = new();
#endif

    public static bool GetAllowNegative(BindableObject view)
        => (bool)view.GetValue(AllowNegativeProperty);

    public static void SetAllowNegative(BindableObject view, bool value)
        => view.SetValue(AllowNegativeProperty, value);

    private static void OnAllowNegativeChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is Entry entry)
            Apply(entry);
    }

    public static void Apply(Entry entry)
    {
#if IOS || MACCATALYST
        if (!GetAllowNegative(entry))
            return;

        if (entry.Handler is not Microsoft.Maui.Handlers.EntryHandler handler)
            return;

        var field = handler.PlatformView;
        field.KeyboardType = UIKit.UIKeyboardType.NumbersAndPunctuation;
        field.SmartDashesType = UIKit.UITextSmartDashesType.No;
        field.SmartQuotesType = UIKit.UITextSmartQuotesType.No;
        field.SmartInsertDeleteType = UIKit.UITextSmartInsertDeleteType.No;

        if (!SelectAllHooked.TryAdd(field, new object()))
            return;

        // Keyboard.Numeric keeps the typed minus in the native field but does not
        // push it into Entry.Text when the field already has a value. Save then
        // still sees the previous text or a partial "-".
        field.EditingChanged += (_, _) => CopyNativeText(entry, field);
        field.EditingDidEnd += (_, _) => CopyNativeText(entry, field);
        field.EditingDidBegin += (_, _) =>
            field.BeginInvokeOnMainThread(() => field.SelectAll(null));
#endif
    }

#if IOS || MACCATALYST
    private static void CopyNativeText(Entry entry, UIKit.UITextField field)
    {
        var native = field.Text ?? string.Empty;
        if (entry.Text != native)
            entry.Text = native;
    }
#endif
}
