namespace Finanzuebersicht.Controls;

/// <summary>
/// Marks a page while a toolkit popup is presented on top of it.
/// On iOS that presentation calls <c>OnDisappearing</c> even though the page stays in place.
/// </summary>
public static class TransientPageOverlay
{
    public static readonly BindableProperty DepthProperty =
        BindableProperty.CreateAttached("Depth", typeof(int), typeof(TransientPageOverlay), 0);

    public static int GetDepth(BindableObject view)
        => (int)view.GetValue(DepthProperty);

    public static bool IsActive(Page page)
        => GetDepth(page) > 0;

    public static void Push(Page page)
        => page.SetValue(DepthProperty, GetDepth(page) + 1);

    public static void Pop(Page page)
    {
        var depth = GetDepth(page);
        if (depth > 0)
            page.SetValue(DepthProperty, depth - 1);
    }
}
