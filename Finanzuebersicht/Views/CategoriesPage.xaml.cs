using Finanzuebersicht.Helpers;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Services;
using Finanzuebersicht.ViewModels;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Views;

public partial class CategoriesPage : BaseContentPage
{
    private readonly ILogger<CategoriesPage>? _logger;

    public CategoriesPage(CategoriesViewModel viewModel, ILogger<CategoriesPage>? logger = null)
    {
        _logger = logger;
        try
        {
            InitializeComponent();
            BindingContext = viewModel;
            AttachDesktopToolbar(viewModel);
        }
        catch (Exception ex)
        {
            CrashLog.Write("CategoriesPage InitializeComponent failed", ex);
            _logger?.LogError(ex, "CategoriesPage InitializeComponent failed");
            Content = CreateErrorContent("Init", ex);
        }
    }

    void AttachDesktopToolbar(CategoriesViewModel viewModel)
    {
        var loc = LocalizationResourceManager.Current;
        DesktopChrome.AddToolbarItems(
            this,
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Aktualisieren], viewModel.LoadKategorienCommand, priority: 0),
            DesktopChrome.CreateToolbarItem(loc[ResourceKeys.Btn_Hinzufuegen], viewModel.GoToDetailCommand, priority: 1));
    }

    protected override void OnAppearing()
    {
        try
        {
            base.OnAppearing();
        }
        catch (Exception ex)
        {
            CrashLog.Write("CategoriesPage OnAppearing failed", ex);
            _logger?.LogError(ex, "CategoriesPage OnAppearing failed");
            Content = CreateErrorContent("OnAppearing", ex);
        }
    }

    private static View CreateErrorContent(string phase, Exception ex) =>
        new ScrollView
        {
            Padding = 20,
            Content = new Label
            {
                Text = $"Verwaltung-Fehler ({phase}):\n\n{ex}",
                FontSize = 13
            }
        };
}
