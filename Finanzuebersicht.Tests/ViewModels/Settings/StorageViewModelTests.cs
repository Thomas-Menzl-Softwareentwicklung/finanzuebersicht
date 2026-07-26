using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Tests.TestHelpers;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class StorageViewModelTests
{
    [Fact]
    public void Constructor_LoadsDataPathFromSettings()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests), ("DataPath", "/Users/test/custom-data"));

        var sut = new StorageViewModel(
            settingsScope.Settings,
            CreateDialogService(),
            CreateLocalizationService());

        Assert.Equal("/Users/test/custom-data", sut.DataPath);
        Assert.False(sut.RequiresRestart);
    }

    [Fact]
    public void Constructor_UsesDefaultPath_WhenSettingsEmpty()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests), ("DataPath", string.Empty));

        var sut = new StorageViewModel(
            settingsScope.Settings,
            CreateDialogService(),
            CreateLocalizationService());

        Assert.Equal(AppPaths.GetDefaultDataDir(), sut.DataPath);
        Assert.False(string.IsNullOrWhiteSpace(sut.DataPath));
        Assert.False(sut.RequiresRestart);
    }

    [Fact]
    public void Constructor_ShowsPendingPath_WhenRestartRequired()
    {
        using var settingsScope = new SettingsScope(
            nameof(StorageViewModelTests),
            ("DataPath", "/Users/test/active"),
            (SettingsKeys.DataPathPending, "/Users/test/pending"));

        var sut = new StorageViewModel(
            settingsScope.Settings,
            CreateDialogService(),
            CreateLocalizationService());

        Assert.Equal("/Users/test/pending", sut.DataPath);
        Assert.True(sut.RequiresRestart);
        Assert.Equal("/Users/test/active", settingsScope.Settings.Get(SettingsKeys.DataPath));
    }

    [Fact]
    public async Task ResetDataPath_SetsPendingDefault_KeepsActiveDataPath()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests), ("DataPath", "/Users/test/custom-data"));
        var dialogService = CreateDialogService();
        var sut = new StorageViewModel(
            settingsScope.Settings,
            dialogService,
            CreateLocalizationService());

        await sut.ResetDataPathCommand.ExecuteAsync(null);

        Assert.Equal("/Users/test/custom-data", settingsScope.Settings.Get(SettingsKeys.DataPath));
        Assert.True(settingsScope.Settings.Contains(SettingsKeys.DataPathPending));
        Assert.Equal(string.Empty, settingsScope.Settings.Get(SettingsKeys.DataPathPending));
        Assert.Equal(AppPaths.GetDefaultDataDir(), sut.DataPath);
        Assert.True(sut.RequiresRestart);
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Stn_SpeicherortZurueckgesetzt,
            ResourceKeys.Stn_SpeicherortZurueckgesetztDesc,
            ResourceKeys.Btn_OK);
    }

    [Fact]
    public async Task ChooseDataPath_WhenPickerReturnsNull_DoesNothing()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests));
        var dialogService = CreateDialogService();
        var folderPicker = Substitute.For<IFolderPicker>();
        folderPicker.PickAsync().Returns(Task.FromResult<string?>(null));
        var sut = new StorageViewModel(
            settingsScope.Settings,
            dialogService,
            CreateLocalizationService(),
            folderPicker);

        await sut.ChooseDataPathCommand.ExecuteAsync(null);

        Assert.Equal("__missing__", settingsScope.Settings.Get("DataPath", "__missing__"));
        Assert.False(settingsScope.Settings.Contains(SettingsKeys.DataPathPending));
        await dialogService.DidNotReceive().ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>());
    }

    [Fact]
    public async Task ChooseDataPath_WhenFolderPickerIsNull_ShowsErrorAlert()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests));
        var dialogService = CreateDialogService();
        var sut = new StorageViewModel(
            settingsScope.Settings,
            dialogService,
            CreateLocalizationService(),
            folderPicker: null);

        await sut.ChooseDataPathCommand.ExecuteAsync(null);

        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Err_Titel,
            ResourceKeys.Msg_BackupServiceNotAvailable,
            ResourceKeys.Btn_OK);
    }

    [Fact]
    public async Task ChooseDataPath_WhenPickerReturnsTempPath_ShowsError()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests));
        var dialogService = CreateDialogService();
        var folderPicker = Substitute.For<IFolderPicker>();
        folderPicker.PickAsync().Returns(Task.FromResult<string?>(Path.Combine(Path.GetTempPath(), "something")));
        var sut = new StorageViewModel(
            settingsScope.Settings,
            dialogService,
            CreateLocalizationService(),
            folderPicker);

        await sut.ChooseDataPathCommand.ExecuteAsync(null);

        Assert.Equal("__missing__", settingsScope.Settings.Get("DataPath", "__missing__"));
        Assert.False(settingsScope.Settings.Contains(SettingsKeys.DataPathPending));
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Stn_UngueltigerOrdner,
            ResourceKeys.Stn_UngueltigerOrdnerDesc,
            ResourceKeys.Btn_OK);
    }

    [Fact]
    public async Task ChooseDataPath_SetsPending_KeepsActiveDataPath()
    {
        using var settingsScope = new SettingsScope(
            nameof(StorageViewModelTests),
            (SettingsKeys.DataPath, "/Users/test/active"));
        var dialogService = CreateDialogService();
        var folderPicker = Substitute.For<IFolderPicker>();
        const string newPath = "/Users/test/new-storage";
        folderPicker.PickAsync().Returns(Task.FromResult<string?>(newPath));
        var sut = new StorageViewModel(
            settingsScope.Settings,
            dialogService,
            CreateLocalizationService(),
            folderPicker);

        await sut.ChooseDataPathCommand.ExecuteAsync(null);

        Assert.Equal("/Users/test/active", settingsScope.Settings.Get(SettingsKeys.DataPath));
        Assert.Equal(newPath, settingsScope.Settings.Get(SettingsKeys.DataPathPending));
        Assert.Equal(newPath, sut.DataPath);
        Assert.True(sut.RequiresRestart);
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Stn_SpeicherortGeaendert,
            ResourceKeys.Stn_SpeicherortGeaendertDesc,
            ResourceKeys.Btn_OK);
    }

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
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.Arg<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAt<string>(0));
        return localizationService;
    }
}
