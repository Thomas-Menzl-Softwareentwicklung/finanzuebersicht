using System.Windows.Input;

namespace Finanzuebersicht.Controls;

[ContentProperty(nameof(FormContent))]
public partial class CreateFormCard : ContentView
{
    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CreateFormCard), string.Empty,
            propertyChanged: OnTitleChanged);

    public static readonly BindableProperty FormContentProperty =
        BindableProperty.Create(nameof(FormContent), typeof(View), typeof(CreateFormCard), null);

    public static readonly BindableProperty CancelTextProperty =
        BindableProperty.Create(nameof(CancelText), typeof(string), typeof(CreateFormCard), string.Empty);

    public static readonly BindableProperty SaveTextProperty =
        BindableProperty.Create(nameof(SaveText), typeof(string), typeof(CreateFormCard), string.Empty);

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(CreateFormCard));

    public static readonly BindableProperty SaveCommandProperty =
        BindableProperty.Create(nameof(SaveCommand), typeof(ICommand), typeof(CreateFormCard));

    public static readonly BindableProperty IsSaveEnabledProperty =
        BindableProperty.Create(nameof(IsSaveEnabled), typeof(bool), typeof(CreateFormCard), true);

    public static readonly BindableProperty ScrollFormContentProperty =
        BindableProperty.Create(nameof(ScrollFormContent), typeof(bool), typeof(CreateFormCard), false,
            propertyChanged: OnScrollSettingsChanged);

    public static readonly BindableProperty MaxFormHeightProperty =
        BindableProperty.Create(nameof(MaxFormHeight), typeof(double), typeof(CreateFormCard), -1d,
            propertyChanged: OnScrollSettingsChanged);

    public CreateFormCard()
    {
        InitializeComponent();
        UpdateScrollBehavior();
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public View? FormContent
    {
        get => (View?)GetValue(FormContentProperty);
        set => SetValue(FormContentProperty, value);
    }

    public string CancelText
    {
        get => (string)GetValue(CancelTextProperty);
        set => SetValue(CancelTextProperty, value);
    }

    public string SaveText
    {
        get => (string)GetValue(SaveTextProperty);
        set => SetValue(SaveTextProperty, value);
    }

    public ICommand? CancelCommand
    {
        get => (ICommand?)GetValue(CancelCommandProperty);
        set => SetValue(CancelCommandProperty, value);
    }

    public ICommand? SaveCommand
    {
        get => (ICommand?)GetValue(SaveCommandProperty);
        set => SetValue(SaveCommandProperty, value);
    }

    public bool IsSaveEnabled
    {
        get => (bool)GetValue(IsSaveEnabledProperty);
        set => SetValue(IsSaveEnabledProperty, value);
    }

    public bool ScrollFormContent
    {
        get => (bool)GetValue(ScrollFormContentProperty);
        set => SetValue(ScrollFormContentProperty, value);
    }

    public double MaxFormHeight
    {
        get => (double)GetValue(MaxFormHeightProperty);
        set => SetValue(MaxFormHeightProperty, value);
    }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    public bool HasActions => CancelCommand is not null || SaveCommand is not null;

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(CancelCommand) or nameof(SaveCommand))
            OnPropertyChanged(nameof(HasActions));
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard card)
            card.OnPropertyChanged(nameof(HasTitle));
    }

    private static void OnScrollSettingsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard card)
            card.UpdateScrollBehavior();
    }

    private void UpdateScrollBehavior()
    {
        if (FormScroll is null)
            return;

        FormScroll.VerticalScrollBarVisibility = ScrollFormContent
            ? ScrollBarVisibility.Always
            : ScrollBarVisibility.Never;

        if (ScrollFormContent && MaxFormHeight > 0)
            FormScroll.HeightRequest = MaxFormHeight;
        else
            FormScroll.ClearValue(HeightRequestProperty);
    }
}
