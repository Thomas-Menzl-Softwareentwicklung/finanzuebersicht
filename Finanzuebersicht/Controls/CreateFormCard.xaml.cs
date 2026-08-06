using System.Windows.Input;

namespace Finanzuebersicht.Controls;

[ContentProperty(nameof(FormContent))]
public partial class CreateFormCard : ContentView
{
    private bool _isLoaded;

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(CreateFormCard), string.Empty,
            propertyChanged: OnTitleChanged);

    public static readonly BindableProperty FormContentProperty =
        BindableProperty.Create(nameof(FormContent), typeof(View), typeof(CreateFormCard), null,
            propertyChanged: OnFormContentChanged);

    public static readonly BindableProperty CancelTextProperty =
        BindableProperty.Create(nameof(CancelText), typeof(string), typeof(CreateFormCard), string.Empty,
            propertyChanged: OnCancelTextChanged);

    public static readonly BindableProperty SaveTextProperty =
        BindableProperty.Create(nameof(SaveText), typeof(string), typeof(CreateFormCard), string.Empty,
            propertyChanged: OnSaveTextChanged);

    public static readonly BindableProperty CancelCommandProperty =
        BindableProperty.Create(nameof(CancelCommand), typeof(ICommand), typeof(CreateFormCard),
            propertyChanged: OnCancelCommandChanged);

    public static readonly BindableProperty SaveCommandProperty =
        BindableProperty.Create(nameof(SaveCommand), typeof(ICommand), typeof(CreateFormCard),
            propertyChanged: OnSaveCommandChanged);

    public static readonly BindableProperty IsSaveEnabledProperty =
        BindableProperty.Create(nameof(IsSaveEnabled), typeof(bool), typeof(CreateFormCard), true,
            propertyChanged: OnIsSaveEnabledChanged);

    public static readonly BindableProperty ScrollFormContentProperty =
        BindableProperty.Create(nameof(ScrollFormContent), typeof(bool), typeof(CreateFormCard), false,
            propertyChanged: OnScrollSettingsChanged);

    public static readonly BindableProperty MaxFormHeightProperty =
        BindableProperty.Create(nameof(MaxFormHeight), typeof(double), typeof(CreateFormCard), -1d,
            propertyChanged: OnScrollSettingsChanged);

    public static readonly BindableProperty AccessibilityDescriptionProperty =
        BindableProperty.Create(nameof(AccessibilityDescription), typeof(string), typeof(CreateFormCard), string.Empty,
            propertyChanged: OnAccessibilityDescriptionChanged);

    public CreateFormCard()
    {
        InitializeComponent();
        // Defer wiring FormContent until Loaded — applying it during parent
        // InitializeComponent deadlocks on Mac Catalyst / UIScene.
        Loaded += OnLoadedOnce;
    }

    private void OnLoadedOnce(object? sender, EventArgs e)
    {
        Loaded -= OnLoadedOnce;
        _isLoaded = true;
        ApplyThemeColors();
        UpdateTitle();
        UpdateCancelText();
        UpdateSaveText();
        UpdateCancelCommand();
        UpdateSaveCommand();
        UpdateIsSaveEnabled();
        UpdateActionsVisibility();
        UpdateScrollBehavior();
        ApplyFormContent(FormContent);
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

    public string AccessibilityDescription
    {
        get => (string)GetValue(AccessibilityDescriptionProperty);
        set => SetValue(AccessibilityDescriptionProperty, value);
    }

    public bool HasTitle => !string.IsNullOrWhiteSpace(Title);

    public bool HasActions => CancelCommand is not null || SaveCommand is not null;

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName is nameof(CancelCommand) or nameof(SaveCommand))
        {
            OnPropertyChanged(nameof(HasActions));
            if (_isLoaded)
                UpdateActionsVisibility();
        }
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
        {
            card.OnPropertyChanged(nameof(HasTitle));
            card.UpdateTitle();
        }
    }

    private static void OnFormContentChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.ApplyFormContent(newValue as View);
    }

    private static void OnCancelTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.UpdateCancelText();
    }

    private static void OnSaveTextChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.UpdateSaveText();
    }

    private static void OnCancelCommandChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.UpdateCancelCommand();
    }

    private static void OnSaveCommandChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.UpdateSaveCommand();
    }

    private static void OnIsSaveEnabledChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.UpdateIsSaveEnabled();
    }

    private static void OnScrollSettingsChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard { _isLoaded: true } card)
            card.UpdateScrollBehavior();
    }

    private static void OnAccessibilityDescriptionChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CreateFormCard card)
            SemanticProperties.SetDescription(card, (string)newValue);
    }

    public void FocusForm()
    {
        if (FormContent is Element formRoot)
            FormFocusHelper.TryFocusFirstInput(formRoot);
        else if (FormPresenter?.Content is Element presenterRoot)
            FormFocusHelper.TryFocusFirstInput(presenterRoot);
    }

    private void ApplyFormContent(View? content)
    {
        if (FormPresenter is null)
            return;

        FormPresenter.Content = content;
    }

    private void UpdateTitle()
    {
        if (TitleLabel is null)
            return;

        TitleLabel.Text = Title;
        TitleLabel.IsVisible = HasTitle;
    }

    private void UpdateCancelText()
    {
        if (CancelButton is null)
            return;

        CancelButton.Text = CancelText;
        SemanticProperties.SetHint(CancelButton, CancelText);
    }

    private void UpdateSaveText()
    {
        if (SaveButton is null)
            return;

        SaveButton.Text = SaveText;
        SemanticProperties.SetHint(SaveButton, SaveText);
    }

    private void UpdateCancelCommand()
    {
        if (CancelButton is not null)
            CancelButton.Command = CancelCommand;
    }

    private void UpdateSaveCommand()
    {
        if (SaveButton is not null)
            SaveButton.Command = SaveCommand;
    }

    private void UpdateIsSaveEnabled()
    {
        if (SaveButton is not null)
            SaveButton.IsEnabled = IsSaveEnabled;
    }

    private void UpdateActionsVisibility()
    {
        if (ActionsGrid is not null)
            ActionsGrid.IsVisible = HasActions;
    }

    private void ApplyThemeColors()
    {
        if (CardBorder is null || CancelButton is null || SaveButton is null)
            return;

        var app = Microsoft.Maui.Controls.Application.Current;
        if (app is not null)
            app.RequestedThemeChanged += (_, _) => ApplyThemeColors();

        var isDark = app?.RequestedTheme == AppTheme.Dark;
        // Keep in sync with Styles.xaml Primary / CardBackground resources.
        CardBorder.Stroke = Color.FromArgb(isDark ? "#0A84FF" : "#007AFF");
        CardBorder.BackgroundColor = Color.FromArgb(isDark ? "#1C1C1E" : "#FFFFFF");
        CancelButton.BackgroundColor = Color.FromArgb(isDark ? "#2C2C2E" : "#E5E5EA");
        CancelButton.TextColor = Color.FromArgb(isDark ? "#FFFFFF" : "#000000");
        SaveButton.BackgroundColor = Color.FromArgb(isDark ? "#0A84FF" : "#007AFF");
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
