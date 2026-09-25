namespace Finanzuebersicht.Core.Services;

public static class CsvImportProfileMatcher
{
    public static CsvImportProfile? Find(CsvTable table, IReadOnlyList<CsvImportProfile> userProfiles)
    {
        foreach (var profile in userProfiles)
        {
            if (CsvImportFingerprint.Matches(table, profile))
                return profile;
        }

        if (CsvImportFingerprint.Matches(table, DkbCsvImportProfile.Instance))
            return DkbCsvImportProfile.Instance;

        return null;
    }
}
