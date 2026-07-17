using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.ViewModels;

public partial class StorageViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _loc;
    private readonly IFolderPicker? _folderPicker;

    [ObservableProperty]
    private string dataPath = string.Empty;

    public StorageViewModel(
        ISettingsService settings,
        IDialogService dialogService,
        ILocalizationService localizationService,
        IFolderPicker? folderPicker = null)
    {
        _settings = settings;
        _dialogService = dialogService;
        _loc = localizationService;
        _folderPicker = folderPicker;

        DataPath = _settings.Get(SettingsKeys.DataPath, "");
        if (string.IsNullOrWhiteSpace(DataPath))
        {
            DataPath = GetDefaultDataDir();
        }
    }

    [RelayCommand]
    private async Task ChooseDataPath()
    {
        if (_folderPicker == null)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Msg_BackupServiceNotAvailable),
                _loc.GetString(ResourceKeys.Btn_OK));
            return;
        }

        try
        {
            var newPath = await _folderPicker.PickAsync();
            if (newPath == null)
            {
                return;
            }

            var tempPath = Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar);
            if (newPath.StartsWith(tempPath, StringComparison.OrdinalIgnoreCase) ||
                newPath.Contains(Path.Combine("var", "folders"), StringComparison.OrdinalIgnoreCase))
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Stn_UngueltigerOrdner),
                    _loc.GetString(ResourceKeys.Stn_UngueltigerOrdnerDesc),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            _settings.Set(SettingsKeys.DataPath, newPath);
            DataPath = newPath;

            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Stn_SpeicherortGeaendert),
                _loc.GetString(ResourceKeys.Stn_SpeicherortGeaendertDesc, newPath),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        catch (Exception ex)
        {
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                _loc.GetString(ResourceKeys.Err_OrdnerNichtWaehlbar, ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task ResetDataPath()
    {
        _settings.Set(SettingsKeys.DataPath, string.Empty);
        DataPath = GetDefaultDataDir();

        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Stn_SpeicherortZurueckgesetzt),
            _loc.GetString(ResourceKeys.Stn_SpeicherortZurueckgesetztDesc),
            _loc.GetString(ResourceKeys.Btn_OK));
    }

    private static string GetDefaultDataDir() => AppPaths.GetDefaultDataDir();
}
