using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvImportProfileMatcherTests
{
    private static CsvTable TableFor(CsvImportProfile profile) => new()
    {
        Delimiter = profile.Delimiter,
        EncodingName = "utf-8",
        HeaderRowIndex = 0,
        Headers = profile.Headers,
        DataRows = [],
        AllRows = []
    };

    [Fact]
    public void Find_EmptyUsers_DkbHeaders_ReturnsBuiltIn()
    {
        var found = CsvImportProfileMatcher.Find(TableFor(DkbCsvImportProfile.Instance), []);
        Assert.Same(DkbCsvImportProfile.Instance, found);
    }

    [Fact]
    public void Find_UserWithSameFingerprint_WinsOverDkb()
    {
        var user = new CsvImportProfile
        {
            Id = "user-1",
            Name = "custom",
            Delimiter = ';',
            Headers = DkbCsvImportProfile.Instance.Headers,
            Columns = DkbCsvImportProfile.Instance.Columns with { Title = "Zahlungspflichtige*r" }
        };
        var found = CsvImportProfileMatcher.Find(TableFor(DkbCsvImportProfile.Instance), [user]);
        Assert.Equal("user-1", found!.Id);
        Assert.Equal("Zahlungspflichtige*r", found.Columns.Title);
    }

    [Fact]
    public void Find_UnknownHeaders_ReturnsNull()
    {
        var table = new CsvTable
        {
            Delimiter = ',',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Date", "Amount", "Text"],
            DataRows = [],
            AllRows = []
        };
        Assert.Null(CsvImportProfileMatcher.Find(table, []));
    }
}
