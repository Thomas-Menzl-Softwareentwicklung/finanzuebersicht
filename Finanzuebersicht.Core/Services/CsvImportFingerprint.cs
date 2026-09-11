namespace Finanzuebersicht.Core.Services;

public static class CsvImportFingerprint
{
    public static string Normalize(string header) => header.Trim().ToLowerInvariant();

    public static string Compute(char delimiter, IReadOnlyList<string> headers) =>
        delimiter + "\n" + string.Join('\n', headers.Select(Normalize));

    public static bool Matches(CsvTable table, CsvImportProfile profile) =>
        table.Delimiter == profile.Delimiter
        && Compute(table.Delimiter, table.Headers) == Compute(profile.Delimiter, profile.Headers);
}
