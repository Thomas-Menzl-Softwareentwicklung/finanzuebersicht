using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class BackupListViewModel : ObservableObject, IAutoLoadViewModel
{
    private readonly ListBackupsUseCase _listBackupsUseCase;
    private readonly RestoreBackupUseCase _restoreBackupUseCase;
    private readonly ISettingsService _settings;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _loc;
    private readonly INavigationService _navigationService;
    private readonly ILogger<BackupListViewModel>? _logger;

    public System.Windows.Input.ICommand AutoLoadCommand => LoadBackupsCommand;

    [ObservableProperty]
    private ObservableCollection<BackupMetadata> backups = [];

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private bool isEmpty;

    public BackupListViewModel(
        ListBackupsUseCase listBackupsUseCase,
        RestoreBackupUseCase restoreBackupUseCase,
        ISettingsService settings,
        IDialogService dialogService,
        ILocalizationService localizationService,
        INavigationService navigationService,
        ILogger<BackupListViewModel>? logger = null)
    {
        _listBackupsUseCase = listBackupsUseCase;
        _restoreBackupUseCase = restoreBackupUseCase;
        _settings = settings;
        _dialogService = dialogService;
        _loc = localizationService;
        _navigationService = navigationService;
        _logger = logger;
    }

    [RelayCommand]
    private async Task LoadBackups()
    {
        if (IsLoading) return;
        IsLoading = true;
        try
        {
            var backupPath = _settings.GetBackupPath();
            var result = await _listBackupsUseCase.ExecuteAsync(backupPath);
            if (!result.IsSuccess)
            {
                _logger?.LogWarning("BackupListViewModel: list failed {Code}", result.Error?.Code);
                Backups = [];
                IsEmpty = true;
                return;
            }

            Backups = new ObservableCollection<BackupMetadata>(result.Value!);
            IsEmpty = Backups.Count == 0;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BackupListViewModel: {Context}", nameof(LoadBackups));
            Backups = [];
            IsEmpty = true;
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private async Task RestoreBackup(BackupMetadata backup)
    {
        var dateDisplay = backup.CreatedAt.ToLocalTime().ToString("g");
        var confirmed = await _dialogService.ShowConfirmationAsync(
            _loc.GetString(ResourceKeys.Msg_BackupRestoreConfirmTitle),
            string.Format(_loc.GetString(ResourceKeys.Msg_BackupRestoreConfirmBody), dateDisplay),
            _loc.GetString(ResourceKeys.Btn_Ja),
            _loc.GetString(ResourceKeys.Btn_Abbrechen));

        if (!confirmed) return;

        try
        {
            var backupPath = _settings.GetBackupPath();
            var result = await _restoreBackupUseCase.ExecuteAsync(backupPath, backup.Id);
            if (result.IsSuccess)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_RestoreSuccessTitle),
                    _loc.GetString(ResourceKeys.Msg_RestoreSuccessDesc),
                    _loc.GetString(ResourceKeys.Btn_OK));
                await _navigationService.GoBackAsync();
                return;
            }

            if (result.Error!.Code == UseCaseErrorCode.BackupDataInconsistent)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_RestoreInconsistentTitle),
                    _loc.GetString(ResourceKeys.Msg_RestoreInconsistentDesc),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            await UseCaseErrorPresenter.ShowAsync(_dialogService, _loc, result.Error);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BackupListViewModel: RestoreBackup failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                string.Format(_loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen), ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }
}
