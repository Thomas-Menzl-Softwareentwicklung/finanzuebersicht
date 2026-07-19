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

    [ObservableProperty]
    private bool requiresRestart;

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

        RefreshDisplayedPath();
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

            // Defer activation until next start so store singletons keep writing to the active path.
            _settings.Set(SettingsKeys.DataPathPending, newPath);
            RefreshDisplayedPath();

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
        // Empty pending = reset to default on next start; keep active DataPath unchanged this session.
        _settings.Set(SettingsKeys.DataPathPending, string.Empty);
        RefreshDisplayedPath();

        await _dialogService.ShowAlertAsync(
            _loc.GetString(ResourceKeys.Stn_SpeicherortZurueckgesetzt),
            _loc.GetString(ResourceKeys.Stn_SpeicherortZurueckgesetztDesc),
            _loc.GetString(ResourceKeys.Btn_OK));
    }

    private void RefreshDisplayedPath()
    {
        RequiresRestart = _settings.Contains(SettingsKeys.DataPathPending);

        if (RequiresRestart)
        {
            var pending = _settings.Get(SettingsKeys.DataPathPending, "");
            DataPath = string.IsNullOrWhiteSpace(pending)
                ? GetDefaultDataDir()
                : pending;
            return;
        }

        var active = _settings.Get(SettingsKeys.DataPath, "");
        DataPath = string.IsNullOrWhiteSpace(active)
            ? GetDefaultDataDir()
            : active;
    }

    private static string GetDefaultDataDir() => AppPaths.GetDefaultDataDir();
}
