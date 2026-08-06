using System.Collections;
using CommunityToolkit.Maui.Views;
using Finanzuebersicht.Converters;
using Finanzuebersicht.Selection;
using Microsoft.Maui.Controls.Shapes;

namespace Finanzuebersicht.Views.Popups;

public class SelectionPopup : Popup<object>
{
    private const int MaxVisibleRows = 5;
    private const double ListVerticalPadding = 6;
    private bool _isClosing;

    public SelectionPopup(IEnumerable items, object? selectedItem, string displayMemberPath)
    {
        BackgroundColor = Colors.Transparent;
        Padding = 0;
        Margin = new Thickness(20);
        CanBeDismissedByTappingOutsideOfPopup = true;
        Content = CreateContent(items, selectedItem, displayMemberPath);
    }

    private View CreateContent(IEnumerable items, object? selectedItem, string displayMemberPath)
    {
        var rows = items
            .Cast<object>()
            .Select(item => new SelectionRow(
                item,
                LocalizedSelectionDisplay.GetDisplayText(item, displayMemberPath),
                ReferenceEquals(item, selectedItem) || Equals(item, selectedItem)))
            .ToList();

        var list = new VerticalStackLayout
        {
            Spacing = 0,
            Padding = new Thickness(0, ListVerticalPadding)
        };
        foreach (var row in rows)
            list.Children.Add(SelectionRowViewFactory.CreateRow(row, SelectRowAsync));

        var rowHeight = SelectionRowViewFactory.RowHeightRequest;
        var visibleRows = Math.Min(rows.Count, MaxVisibleRows);
        var isScrollable = rows.Count > MaxVisibleRows;
        var scrollHeight = visibleRows * rowHeight + (ListVerticalPadding * 2);

        var cardBackground = ColorResourceHelper.GetThemeColor(
            "CardBackground", "CardBackgroundDark",
            Color.FromArgb("#FFFFFF"), Color.FromArgb("#1C1C1E"));

        var scroll = new ScrollView
        {
            HeightRequest = scrollHeight,
            VerticalScrollBarVisibility = ScrollBarVisibility.Always,
            Content = list
        };

        var body = new VerticalStackLayout { Spacing = 0 };
        body.Children.Add(scroll);

        if (isScrollable)
        {
            body.Children.Add(new BoxView
            {
                HeightRequest = 0.5,
                Color = ColorResourceHelper.GetThemeColor(
                    "Separator", "SeparatorDark",
                    Color.FromArgb("#E5E5EA"), Color.FromArgb("#38383A"))
            });

            body.Children.Add(new Label
            {
                Text = "▼",
                FontSize = 10,
                HorizontalTextAlignment = TextAlignment.Center,
                Padding = new Thickness(0, 6, 0, 10),
                TextColor = ColorResourceHelper.GetThemeColor(
                    "Gray500", "Gray600",
                    Color.FromArgb("#8E8E93"), Color.FromArgb("#8E8E93"))
            });
        }

        return new Border
        {
            Padding = 0,
            BackgroundColor = cardBackground,
            StrokeThickness = 0,
            StrokeShape = new RoundRectangle { CornerRadius = 10 },
            Shadow = new Shadow
            {
                Brush = Brush.Black,
                Offset = new Point(0, 6),
                Radius = 16,
                Opacity = 0.28f
            },
            Content = new Grid
            {
                WidthRequest = 300,
                Children = { body }
            }
        };
    }

    private async Task SelectRowAsync(SelectionRow row)
    {
        if (_isClosing)
            return;

        _isClosing = true;
        await CloseAsync(row.Item);
    }
}
