using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Infrastructure.Services;

namespace Finanzuebersicht.Tests.Infrastructure;

public class FileCsvImportProfileStoreTests
{
    [Fact]
    public async Task UpsertAsync_ThenGet_ReturnsProfile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-csv-profiles-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileCsvImportProfileStore(dir);
            var profile = new CsvImportProfile
            {
                Id = "user-1",
                Name = "My Bank",
                Delimiter = ';',
                Headers = ["Date", "Amount", "Text"],
                Columns = new CsvColumnMapping { Date = "Date", Amount = "Amount", Title = "Text" }
            };
            await store.UpsertAsync(profile);

            var profiles = await store.GetUserProfilesAsync();
            Assert.Single(profiles);
            Assert.Equal("user-1", profiles[0].Id);
            Assert.Equal("Date", profiles[0].Columns.Date);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task ReplaceAllAsync_Empty_ClearsProfiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-csv-profiles-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileCsvImportProfileStore(dir);
            await store.UpsertAsync(new CsvImportProfile
            {
                Id = "user-1",
                Name = "My Bank",
                Delimiter = ';',
                Headers = ["Date"],
                Columns = new CsvColumnMapping { Date = "Date", Amount = "Amount", Title = "Text" }
            });

            await store.ReplaceAllAsync([]);

            var profiles = await store.GetUserProfilesAsync();
            Assert.Empty(profiles);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    [Fact]
    public async Task UpsertAsync_BuiltIn_ClonesAsUserProfile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fu-csv-profiles-" + Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileCsvImportProfileStore(dir);
            await store.UpsertAsync(DkbCsvImportProfile.Instance);

            var profiles = await store.GetUserProfilesAsync();
            Assert.Single(profiles);
            Assert.False(profiles[0].IsBuiltIn);
            Assert.NotEqual("builtin-dkb", profiles[0].Id);
            Assert.Equal(DkbCsvImportProfile.Instance.Delimiter, profiles[0].Delimiter);
            Assert.Equal(DkbCsvImportProfile.Instance.Columns.Date, profiles[0].Columns.Date);
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
