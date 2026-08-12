using Finanzuebersicht.Application.Results;
using Finanzuebersicht.Presentation.Services;
using Finanzuebersicht.Resources.Strings;

namespace Finanzuebersicht.Tests.Presentation.Services;

public class UseCaseErrorPresenterTests
{
    [Fact]
    public void GetMessage_MapsAccountNotFound()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetString(ResourceKeys.Err_KontoNichtGefunden).Returns("Konto fehlt");

        var message = UseCaseErrorPresenter.GetMessage(
            loc,
            new UseCaseError(UseCaseErrorCode.AccountNotFound));

        Assert.Equal("Konto fehlt", message);
    }

    [Fact]
    public void GetMessage_MapsLicenseLimitWithArgs()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetString(ResourceKeys.Err_LimitErreicht, 3, 3).Returns("Limit 3/3");

        var message = UseCaseErrorPresenter.GetMessage(
            loc,
            new UseCaseError(UseCaseErrorCode.LicenseLimitReached, 3, 3));

        Assert.Equal("Limit 3/3", message);
    }

    [Fact]
    public async Task ShowAsync_ShowsLocalizedAlert()
    {
        var loc = Substitute.For<ILocalizationService>();
        loc.GetString(ResourceKeys.Err_Titel).Returns("Fehler");
        loc.GetString(ResourceKeys.Btn_OK).Returns("OK");
        loc.GetString(ResourceKeys.Err_KontoArchiviert).Returns("Archiviert");

        var dialogs = Substitute.For<IDialogService>();
        dialogs.ShowAlertAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(Task.CompletedTask);

        await UseCaseErrorPresenter.ShowAsync(
            dialogs,
            loc,
            new UseCaseError(UseCaseErrorCode.AccountArchived));

        await dialogs.Received(1).ShowAlertAsync("Fehler", "Archiviert", "OK");
    }
}
