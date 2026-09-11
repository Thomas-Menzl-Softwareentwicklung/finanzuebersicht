using Microsoft.Extensions.Logging;

namespace Finanzuebersicht.Infrastructure.Services;

public class FileCsvImportProfileStore(string dataDir, ILogger<FileCsvImportProfileStore>? logger = null)
    : JsonDataStoreBase(dataDir, logger), ICsvImportProfileStore
{
    private string ProfilesFile => Path.Combine(DataDir, DataFileNames.CsvImportProfiles);

    public async Task<IReadOnlyList<CsvImportProfile>> GetUserProfilesAsync()
    {
        await StoreLock.WaitAsync();
        try
        {
            return await LoadAsync<CsvImportProfile>(ProfilesFile);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public async Task UpsertAsync(CsvImportProfile profile)
    {
        var toSave = profile.IsBuiltIn ? CloneAsUserProfile(profile) : profile;

        await StoreLock.WaitAsync();
        try
        {
            var items = await LoadAsync<CsvImportProfile>(ProfilesFile);
            var idx = items.FindIndex(p => p.Id == toSave.Id);
            if (idx >= 0)
                items[idx] = toSave;
            else
                items.Add(toSave);
            await SaveAsync(ProfilesFile, items);
        }
        finally
        {
            StoreLock.Release();
        }
    }

    public Task ReplaceAllAsync(IEnumerable<CsvImportProfile> profiles)
        => ReplaceAllAsync(ProfilesFile, profiles);

    private static CsvImportProfile CloneAsUserProfile(CsvImportProfile profile) => new()
    {
        Id = Guid.NewGuid().ToString(),
        Name = profile.Name,
        IsBuiltIn = false,
        Delimiter = profile.Delimiter,
        Headers = profile.Headers,
        HeaderRowIndex = profile.HeaderRowIndex,
        Columns = profile.Columns,
        DateFormat = profile.DateFormat,
        DecimalStyle = profile.DecimalStyle
    };
}
