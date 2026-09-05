using Finanzuebersicht.Application.UseCases.Backup;
using Finanzuebersicht.Core.Constants;
using Finanzuebersicht.Navigation;
using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Tests.TestHelpers;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class BackupViewModelTests
{
    [Fact]
    public async Task CreateBackup_WhenSuccessful_ShowsSuccessAlert()
    {
        using var settingsScope = new SettingsScope(nameof(BackupViewModelTests));
        var backupService = Substitute.For<IBackupService>();
        backupService.CreateBackupAsync(Arg.Any<string?>())
            .Returns(Task.FromResult(new BackupMetadata
            {
                EntityCounts = new Dictionary<string, int>
                {
                    [BackupEntityKeys.Categories] = 2,
                    [BackupEntityKeys.Transactions] = 3,
                    [BackupEntityKeys.Recurring] = 4
                }
            }));

        var dialogService = CreateDialogService();
        var sut = CreateSut(settingsScope.Settings, backupService, dialogService);

        await sut.CreateBackupCommand.ExecuteAsync(null);

        await backupService.Received(1).CreateBackupAsync(Arg.Any<string?>());
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Msg_BackupSuccessTitle,
            Arg.Any<string>(),
            ResourceKeys.Btn_OK);
    }

    [Fact]
    public async Task ExportAsCSV_WhenFileSaverNull_DoesNothing()
    {
        using var settingsScope = new SettingsScope(nameof(BackupViewModelTests));
        var backupService = Substitute.For<IBackupService>();
        var dialogService = CreateDialogService();
        var sut = CreateSut(
            settingsScope.Settings,
            backupService,
            dialogService,
            fileSaver: null,
            clock: new FixedClock(new DateTime(2026, 3, 15)));

        await sut.ExportAsCSVCommand.ExecuteAsync(null);

        await backupService.DidNotReceive().ExportAsCSVAsync();
        await dialogService.DidNotReceive().ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task RestoreBackup_NavigatesToBackupListRoute()
    {
        using var settingsScope = new SettingsScope(nameof(BackupViewModelTests));
        var navigationService = Substitute.For<INavigationService>();
        var sut = CreateSut(
            settingsScope.Settings,
            Substitute.For<IBackupService>(),
            CreateDialogService(),
            navigationService: navigationService);

        await sut.RestoreBackupCommand.ExecuteAsync(null);

        await navigationService.Received(1).GoToAsync(
            Routes.BackupList,
            Arg.Is<IDictionary<string, object>?>(parameters => parameters == null));
    }

    [Fact]
    public async Task BrowseBackups_WhenNoBackups_ShowsEmptyAlert()
    {
        using var settingsScope = new SettingsScope(nameof(BackupViewModelTests));
        var backupService = Substitute.For<IBackupService>();
        backupService.ListBackupsAsync(Arg.Any<string>())
            .Returns(Task.FromResult<IEnumerable<BackupMetadata>>(Array.Empty<BackupMetadata>()));

        var dialogService = CreateDialogService();
        var sut = CreateSut(settingsScope.Settings, backupService, dialogService);

        await sut.BrowseBackupsCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Msg_NoBackupsTitle,
            ResourceKeys.Msg_NoBackupsDesc,
            ResourceKeys.Btn_OK);
    }

    private static BackupViewModel CreateSut(
        ISettingsService settings,
        IBackupService backupService,
        IDialogService dialogService,
        INavigationService? navigationService = null,
        IFileSaver? fileSaver = null,
        IClock? clock = null)
        => new(
            settings,
            new CreateBackupUseCase(backupService),
            new ListBackupsUseCase(backupService),
            new ExportCsvUseCase(backupService),
            dialogService,
            CreateLocalizationService(),
            navigationService ?? Substitute.For<INavigationService>(),
            fileSaver,
            clock);

    private static IDialogService CreateDialogService()
    {
        var dialogService = Substitute.For<IDialogService>();
        dialogService.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);
        return dialogService;
    }

    private static ILocalizationService CreateLocalizationService()
    {
        var localizationService = Substitute.For<ILocalizationService>();
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));
        return localizationService;
    }
}
