using Finanzuebersicht.Presentation.Services;

namespace Finanzuebersicht.Tests.Presentation.Services;

public class AppEventsTests
{
    [Fact]
    public void NotifyDataChanged_RaisesDataChanged()
    {
        var sut = new AppEvents();
        var raised = 0;
        sut.DataChanged += () => raised++;

        sut.NotifyDataChanged();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void NotifyLanguageChanged_RaisesLanguageChanged()
    {
        var sut = new AppEvents();
        var raised = 0;
        sut.LanguageChanged += () => raised++;

        sut.NotifyLanguageChanged();

        Assert.Equal(1, raised);
    }

    [Fact]
    public void NotifyCurrencyChanged_RaisesCurrencyChanged()
    {
        var sut = new AppEvents();
        var raised = 0;
        sut.CurrencyChanged += () => raised++;

        sut.NotifyCurrencyChanged();

        Assert.Equal(1, raised);
    }
}
