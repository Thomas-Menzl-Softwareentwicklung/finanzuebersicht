using Finanzuebersicht.Resources.Strings;
using Finanzuebersicht.Tests.TestHelpers;
using Finanzuebersicht.ViewModels;

namespace Finanzuebersicht.Tests.ViewModels.Settings;

public class StorageViewModelTests
{
    [Fact]
    public void Constructor_LoadsDataPathFromSettings()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests), (SettingsKeys.DataPath, "/Users/test/custom-data"));

        var sut = new StorageViewModel(
            settingsScope.Settings,
            CreateDialogService(),
            CreateLocalizationService());

        Assert.Equal("/Users/test/custom-data", sut.DataPath);
    }

    [Fact]
    public void Constructor_UsesDefaultPath_WhenSettingsEmpty()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests), (SettingsKeys.DataPath, string.Empty));

        var sut = new StorageViewModel(
            settingsScope.Settings,
            CreateDialogService(),
            CreateLocalizationService());

        Assert.Equal(AppPaths.GetDefaultDataDir(), sut.DataPath);
        Assert.False(string.IsNullOrWhiteSpace(sut.DataPath));
    }

    [Fact]
    public async Task ResetDataPath_ClearsSettingsAndRestoresDefault()
    {
        using var settingsScope = new SettingsScope(nameof(StorageViewModelTests), (SettingsKeys.DataPath, "/Users/test/custom-data"));
        var dialogService = CreateDialogService();
        var sut = new StorageViewModel(
            settingsScope.Settings,
            dialogService,
            CreateLocalizationService());

        await sut.ResetDataPathCommand.ExecuteAsync(null);

        Assert.Equal(string.Empty, settingsScope.Settings.Get(SettingsKeys.DataPath));
        Assert.Equal(AppPaths.GetDefaultDataDir(), sut.DataPath);
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

        Assert.Equal("__missing__", settingsScope.Settings.Get(SettingsKeys.DataPath, "__missing__"));
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

        Assert.Equal("__missing__", settingsScope.Settings.Get(SettingsKeys.DataPath, "__missing__"));
        await dialogService.Received(1).ShowAlertAsync(
            ResourceKeys.Stn_UngueltigerOrdner,
            ResourceKeys.Stn_UngueltigerOrdnerDesc,
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
        localizationService.GetString(Arg.Any<string>()).Returns(call => call.ArgNotNull<string>());
        localizationService.GetString(Arg.Any<string>(), Arg.Any<object[]>()).Returns(call => call.ArgAtNotNull<string>(0));
        return localizationService;
    }

}
