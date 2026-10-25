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

        if (entry.Handler is Microsoft.Maui.Handlers.EntryHandler handler)
            handler.PlatformView.KeyboardType = UIKit.UIKeyboardType.NumbersAndPunctuation;
#endif
    }
}
