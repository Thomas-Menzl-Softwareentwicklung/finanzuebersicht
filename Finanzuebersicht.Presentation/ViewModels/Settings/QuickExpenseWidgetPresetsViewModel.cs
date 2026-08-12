using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Transactions;
using Finanzuebersicht.Core.Licensing;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class QuickExpenseWidgetPresetSlotItem : ObservableObject
{
    public int Slot { get; }

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string amountText = string.Empty;

    public QuickExpenseWidgetPresetSlotItem(int slot)
    {
        Slot = slot;
    }

    public string SlotLabel => $"{Slot + 1}";
}

public partial class QuickExpenseWidgetPresetsViewModel : ObservableObject
{
    private readonly LoadQuickExpenseWidgetPresetsUseCase _loadUseCase;
    private readonly SaveQuickExpenseWidgetPresetsUseCase _saveUseCase;
    private readonly ILocalizationService _loc;
    private readonly IDialogService _dialogService;
    private readonly IFeedbackService _feedbackService;
    private readonly IWidgetTimelineReloader _widgetReloader;
    private readonly ILicenseService _licenseService;
    private readonly ILogger<QuickExpenseWidgetPresetsViewModel>? _logger;

    public ObservableCollection<QuickExpenseWidgetPresetSlotItem> Slots { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private bool isBusy;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowProHint))]
    [NotifyPropertyChangedFor(nameof(CanEdit))]
    private bool hasProAccess;

    public bool ShowProHint => !HasProAccess;
    public bool CanEdit => HasProAccess && !IsBusy;

    public QuickExpenseWidgetPresetsViewModel(
        LoadQuickExpenseWidgetPresetsUseCase loadUseCase,
        SaveQuickExpenseWidgetPresetsUseCase saveUseCase,
        ILocalizationService localizationService,
        IDialogService dialogService,
        IFeedbackService feedbackService,
        IWidgetTimelineReloader? widgetTimelineReloader = null,
        ILicenseService? licenseService = null,
        ILogger<QuickExpenseWidgetPresetsViewModel>? logger = null)
    {
        _loadUseCase = loadUseCase;
        _saveUseCase = saveUseCase;
        _loc = localizationService;
        _dialogService = dialogService;
        _feedbackService = feedbackService;
        _widgetReloader = widgetTimelineReloader ?? NullWidgetTimelineReloader.Instance;
        _licenseService = licenseService ?? UnrestrictedLicenseService.Instance;
        _logger = logger;

        for (var i = 0; i < QuickExpenseWidgetPresetDefaults.SlotCount; i++)
            Slots.Add(new QuickExpenseWidgetPresetSlotItem(i));

        HasProAccess = _licenseService.HasFeature(AppFeature.QuickExpenseCapture);
    }

    public string SectionTitle => _loc.GetString(ResourceKeys.Stn_WidgetShortcutsTitle);
    public string SectionHint => _loc.GetString(ResourceKeys.Stn_WidgetShortcutsHint);
    public string ProHint => _loc.GetString(ResourceKeys.Err_ProErforderlich);
    public string TitlePlaceholder => _loc.GetString(ResourceKeys.Hint_SchnellAusgabeInfo);
    public string AmountPlaceholder => _loc.GetString(ResourceKeys.Hint_Betrag);

    private void ApplyPresetsToSlots(IReadOnlyList<QuickExpenseWidgetPreset> presets)
    {
        for (var i = 0; i < Slots.Count && i < presets.Count; i++)
        {
            Slots[i].Title = presets[i].Title;
            Slots[i].AmountText = FlexibleAmountParser.ToDisplayAmountText(presets[i].AmountText);
        }
    }

    [RelayCommand]
    public async Task LoadAsync()
    {
        HasProAccess = _licenseService.HasFeature(AppFeature.QuickExpenseCapture);
        if (!HasProAccess)
            return;

        try
        {
            IsBusy = true;
            var presets = await _loadUseCase.ExecuteAsync();
            ApplyPresetsToSlots(presets);
        }
        catch (FeatureGateException)
        {
            HasProAccess = false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Load widget presets failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_LadenFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!HasProAccess)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_ProErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            // Snapshot before IsBusy flips CanEdit/IsEnabled — otherwise iOS Entry may not
            // flush TwoWay bindings while still focused, and we would persist stale values.
            var presets = Slots
                .Select(s => new QuickExpenseWidgetPreset(s.Slot, s.Title ?? string.Empty, s.AmountText ?? string.Empty))
                .ToList();

            IsBusy = true;

            var result = await _saveUseCase.ExecuteAsync(presets);
            if (!result.Success)
            {
                var message = result.ValidationError switch
                {
                    TransactionInputError.InvalidAmountFormat => _loc.GetString(ResourceKeys.Err_UngueltigerBetrag),
                    TransactionInputError.AmountMustBePositive => _loc.GetString(ResourceKeys.Err_BetragGroesserNull),
                    TransactionInputError.TitleRequired => _loc.GetString(ResourceKeys.Err_TitelErforderlich),
                    _ => _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, string.Empty)
                };

                if (result.InvalidSlot is int slot)
                    message = $"{_loc.GetString(ResourceKeys.Stn_WidgetShortcutSlot, slot + 1)}: {message}";

                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Err_Titel),
                    message,
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            var saved = await _loadUseCase.ExecuteAsync();
            ApplyPresetsToSlots(saved);
            _widgetReloader.ReloadAll();
            await _feedbackService.ShowSnackbarAsync(_loc.GetString(ResourceKeys.Msg_WidgetShortcutsGespeichert));
        }
        catch (FeatureGateException)
        {
            HasProAccess = false;
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_ProErforderlich),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Save widget presets failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        finally
        {
            IsBusy = false;
        }
    }
}
