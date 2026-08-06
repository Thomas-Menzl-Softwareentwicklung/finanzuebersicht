using System.ComponentModel;
using System.Collections;
using System.Linq;
using CommunityToolkit.Maui.Extensions;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Selection;
using Finanzuebersicht.Services;
using Finanzuebersicht.Views.Popups;
using Microsoft.Maui.Controls;

namespace Finanzuebersicht.Controls;

public partial class SelectionField : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(SelectionField));

    public static readonly BindableProperty SelectedItemProperty =
        BindableProperty.Create(nameof(SelectedItem), typeof(object), typeof(SelectionField), null, BindingMode.TwoWay, propertyChanged: OnSelectionChanged);

    public static readonly BindableProperty DisplayMemberPathProperty =
        BindableProperty.Create(nameof(DisplayMemberPath), typeof(string), typeof(SelectionField), "Name");

    public static readonly BindableProperty PlaceholderProperty =
        BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(SelectionField), string.Empty);

    public static readonly BindableProperty FieldLabelProperty =
        BindableProperty.Create(nameof(FieldLabel), typeof(string), typeof(SelectionField), string.Empty, propertyChanged: OnAccessibilityContextChanged);

    public static readonly BindableProperty ResolvedDisplayTextProperty =
        BindableProperty.Create(nameof(ResolvedDisplayText), typeof(string), typeof(SelectionField), string.Empty);

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public object? SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set => SetValue(SelectedItemProperty, value);
    }

    public string DisplayMemberPath
    {
        get => (string)GetValue(DisplayMemberPathProperty);
        set => SetValue(DisplayMemberPathProperty, value);
    }

    public string Placeholder
    {
        get => (string)GetValue(PlaceholderProperty);
        set => SetValue(PlaceholderProperty, value);
    }

    public string FieldLabel
    {
        get => (string)GetValue(FieldLabelProperty);
        set => SetValue(FieldLabelProperty, value);
    }

    public string ResolvedDisplayText
    {
        get => (string)GetValue(ResolvedDisplayTextProperty);
        set => SetValue(ResolvedDisplayTextProperty, value);
    }

    public SelectionField()
    {
        InitializeComponent();
        UpdateResolvedDisplay();
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        LocalizationResourceManager.Current.PropertyChanged -= OnLocalizationChanged;
        if (Parent is not null)
        {
            LocalizationResourceManager.Current.PropertyChanged += OnLocalizationChanged;
            UpdateResolvedDisplay();
        }
    }

    private void OnLocalizationChanged(object? sender, PropertyChangedEventArgs e)
    {
        UpdateResolvedDisplay();
        UpdateAccessibilityDescription();
    }

    private static void OnSelectionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SelectionField field)
        {
            field.UpdateResolvedDisplay();
            field.UpdateAccessibilityDescription();
        }
    }

    private static void OnAccessibilityContextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is SelectionField field)
            field.UpdateAccessibilityDescription();
    }

    protected override void OnBindingContextChanged()
    {
        base.OnBindingContextChanged();
        UpdateResolvedDisplay();
    }

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);
        if (propertyName is nameof(SelectedItem) or nameof(Placeholder) or nameof(DisplayMemberPath) or nameof(ItemsSource))
            UpdateResolvedDisplay();
    }

    private void UpdateResolvedDisplay()
    {
        if (SelectedItem is not null)
        {
            ResolvedDisplayText = LocalizedSelectionDisplay.GetDisplayText(SelectedItem, DisplayMemberPath);
            UpdateAccessibilityDescription();
            return;
        }

        ResolvedDisplayText = string.IsNullOrWhiteSpace(Placeholder) ? "—" : Placeholder;
        UpdateAccessibilityDescription();
    }

    private void UpdateAccessibilityDescription()
    {
        if (string.IsNullOrWhiteSpace(FieldLabel))
        {
            SemanticProperties.SetDescription(SelectionBorder, ResolvedDisplayText);
            return;
        }

        var hasSelection = SelectedItem is not null;
        var template = LocalizationResourceManager.Current[
            hasSelection ? ResourceKeys.A11y_SelectionField : ResourceKeys.A11y_SelectionFieldLeer];
        SemanticProperties.SetDescription(
            SelectionBorder,
            hasSelection
                ? string.Format(template, FieldLabel, ResolvedDisplayText)
                : string.Format(template, FieldLabel));
    }

    private async void OnTapped(object? sender, TappedEventArgs e)
    {
        if (!IsEnabled || ItemsSource is null || !ItemsSource.Cast<object>().Any())
            return;

        var popup = new SelectionPopup(ItemsSource, SelectedItem, DisplayMemberPath);
        var page = GetParentPage();
        if (page is null)
            return;

        var popupResult = await page.ShowPopupAsync<object>(popup);
        if (popupResult.WasDismissedByTappingOutsideOfPopup || popupResult.Result is null)
            return;

        SelectedItem = popupResult.Result;
        UpdateResolvedDisplay();
    }

    private Page? GetParentPage()
    {
        Element? parent = this;
        while (parent is not null)
        {
            if (parent is Page page)
                return page;
            parent = parent.Parent;
        }

        return Shell.Current?.CurrentPage;
    }
}
