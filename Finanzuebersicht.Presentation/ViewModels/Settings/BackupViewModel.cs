using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;
using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.ViewModels;

public partial class BackupViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly CreateBackupUseCase _createBackupUseCase;
    private readonly ListBackupsUseCase _listBackupsUseCase;
    private readonly ExportCsvUseCase _exportCsvUseCase;
    private readonly IDialogService _dialogService;
    private readonly ILocalizationService _loc;
    private readonly INavigationService _navigationService;
    private readonly IFileSaver? _fileSaver;
    private readonly IClock _clock;
    private readonly ILogger<BackupViewModel>? _logger;

    [ObservableProperty]
    private string lastBackupInfo = string.Empty;

    public BackupViewModel(
        ISettingsService settings,
        CreateBackupUseCase createBackupUseCase,
        ListBackupsUseCase listBackupsUseCase,
        ExportCsvUseCase exportCsvUseCase,
        IDialogService dialogService,
        ILocalizationService localizationService,
        INavigationService navigationService,
        IFileSaver? fileSaver = null,
        IClock? clock = null,
        ILogger<BackupViewModel>? logger = null)
    {
        _settings = settings;
        _createBackupUseCase = createBackupUseCase;
        _listBackupsUseCase = listBackupsUseCase;
        _exportCsvUseCase = exportCsvUseCase;
        _dialogService = dialogService;
        _loc = localizationService;
        _navigationService = navigationService;
        _fileSaver = fileSaver;
        _clock = clock ?? SystemClock.Instance;
        _logger = logger;

        UpdateLastBackupInfo();
    }

    [RelayCommand]
    private async Task CreateBackup()
    {
        try
        {
            var result = await _createBackupUseCase.ExecuteAsync(_settings.GetBackupPath());
            if (!result.IsSuccess)
            {
                await UseCaseErrorPresenter.ShowAsync(_dialogService, _loc, result.Error!);
                return;
            }

            var metadata = result.Value!;
            UpdateLastBackupInfo();

            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_BackupSuccessTitle),
                string.Format(
                    _loc.GetString(ResourceKeys.Msg_BackupCreatedBody),
                    metadata.EntityCounts[BackupEntityKeys.Categories],
                    metadata.EntityCounts[BackupEntityKeys.Transactions],
                    metadata.EntityCounts[BackupEntityKeys.Recurring]),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CreateBackup failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_BackupFailedTitle),
                string.Format(_loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen), ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task BrowseBackups()
    {
        try
        {
            var result = await _listBackupsUseCase.ExecuteAsync(_settings.GetBackupPath());
            if (!result.IsSuccess)
            {
                await UseCaseErrorPresenter.ShowAsync(_dialogService, _loc, result.Error!);
                return;
            }

            var backups = result.Value!;
            if (backups.Count == 0)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_NoBackupsTitle),
                    _loc.GetString(ResourceKeys.Msg_NoBackupsDesc),
                    _loc.GetString(ResourceKeys.Btn_OK));
                return;
            }

            var backupList = string.Join("\n", backups.Take(5).Select(b => $"{b.CreatedAt:g} - {Path.GetFileNameWithoutExtension(b.FileName)}"));
            if (backups.Count > 5)
            {
                backupList += "\n" + _loc.GetString(ResourceKeys.Msg_AndMoreBackups, backups.Count - 5);
            }

            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_AvailableBackupsTitle),
                backupList,
                _loc.GetString(ResourceKeys.Btn_OK));
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "BrowseBackups failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Err_Titel),
                string.Format(_loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen), ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    [RelayCommand]
    private async Task RestoreBackup()
    {
        await _navigationService.GoToAsync(Routes.BackupList);
    }

    [RelayCommand]
    private async Task ExportAsCSV()
    {
        try
        {
            if (_fileSaver == null)
            {
                return;
            }

            var result = await _exportCsvUseCase.ExecuteAsync();
            if (!result.IsSuccess)
            {
                await UseCaseErrorPresenter.ShowAsync(_dialogService, _loc, result.Error!);
                return;
            }

            await using var csvStream = result.Value!;
            csvStream.Seek(0, SeekOrigin.Begin);

            var fileName = $"Finanzuebersicht_Export_{_clock.Now:yyyy-MM-dd}.csv";
            var saveResult = await _fileSaver.SaveAsync(fileName, csvStream, CancellationToken.None);

            if (saveResult.IsSuccessful)
            {
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_CSVExportedTitle),
                    string.Format(_loc.GetString(ResourceKeys.Msg_CSVExportedBody), saveResult.FilePath),
                    _loc.GetString(ResourceKeys.Btn_OK));
            }
            else if (saveResult.Exception is not null and not OperationCanceledException)
            {
                _logger?.LogError(saveResult.Exception, "ExportAsCSV SaveAsync failed");
                await _dialogService.ShowAlertAsync(
                    _loc.GetString(ResourceKeys.Msg_CSVExportFailedTitle),
                    string.Format(_loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen), saveResult.Exception.Message),
                    _loc.GetString(ResourceKeys.Btn_OK));
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ExportAsCSV failed");
            await _dialogService.ShowAlertAsync(
                _loc.GetString(ResourceKeys.Msg_CSVExportFailedTitle),
                string.Format(_loc.GetString(ResourceKeys.Err_SpeichernFehlgeschlagen), ex.Message),
                _loc.GetString(ResourceKeys.Btn_OK));
        }
    }

    private void UpdateLastBackupInfo()
    {
        var lastBackupStr = _settings.Get(SettingsKeys.LastBackupTime, string.Empty);
        if (string.IsNullOrEmpty(lastBackupStr))
        {
            LastBackupInfo = _loc.GetString(ResourceKeys.Stn_NoBackupYet);
            return;
        }

        if (!DateTime.TryParse(lastBackupStr, out var lastBackup))
        {
            LastBackupInfo = _loc.GetString(ResourceKeys.Stn_NoBackupYet);
            return;
        }

        var diff = _clock.UtcNow - lastBackup;
        if (diff.TotalSeconds < 60)
        {
            LastBackupInfo = _loc.GetString(ResourceKeys.Stn_LastBackupSeconds);
        }
        else if (diff.TotalMinutes < 60)
        {
            LastBackupInfo = string.Format(_loc.GetString(ResourceKeys.Stn_LastBackupMinutes), (int)diff.TotalMinutes);
        }
        else
        {
            LastBackupInfo = diff.TotalHours < 24
                ? string.Format(_loc.GetString(ResourceKeys.Stn_LastBackupHours), (int)diff.TotalHours)
                : string.Format(_loc.GetString(ResourceKeys.Stn_LastBackupDays), (int)diff.TotalDays);
        }
    }
}
