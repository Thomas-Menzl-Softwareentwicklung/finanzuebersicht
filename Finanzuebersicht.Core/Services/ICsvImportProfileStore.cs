namespace Finanzuebersicht.Core.Services;

public interface ICsvImportProfileStore
{
    Task<IReadOnlyList<CsvImportProfile>> GetUserProfilesAsync();
    Task UpsertAsync(CsvImportProfile profile);
    Task DeleteAsync(string id);
    Task ReplaceAllAsync(IEnumerable<CsvImportProfile> profiles);
}
