namespace Finanzuebersicht.Controls;

internal static class FormFocusHelper
{
    public static void TryFocusFirstInput(Element? root)
    {
        if (FindFirstFocusable(root) is VisualElement target)
            target.Focus();
    }

    private static VisualElement? FindFirstFocusable(Element? element)
    {
        if (element is null)
            return null;

        if (element is VisualElement visual &&
            visual.IsEnabled &&
            visual.IsVisible &&
            visual is Entry or Editor or SearchBar)
        {
            return visual;
        }

        switch (element)
        {
            case Layout layout:
                foreach (var child in layout.Children)
                {
                    if (child is Element childElement && FindFirstFocusable(childElement) is { } found)
                        return found;
                }
                break;
            case ContentView contentView when contentView.Content is Element content:
                return FindFirstFocusable(content);
            case ScrollView scrollView when scrollView.Content is Element scrollContent:
                return FindFirstFocusable(scrollContent);
            case Border border when border.Content is Element borderContent:
                return FindFirstFocusable(borderContent);
        }

        return null;
    }
}
