namespace Finanzuebersicht.Core.Services;

public sealed class CsvTable
{
    public required char Delimiter { get; init; }
    public required string EncodingName { get; init; }
    public required int HeaderRowIndex { get; init; }
    public required IReadOnlyList<string> Headers { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> DataRows { get; init; }
    public required IReadOnlyList<IReadOnlyList<string>> AllRows { get; init; }

    public CsvTable WithHeaderRow(int headerRowIndex)
    {
        if (headerRowIndex < 0 || headerRowIndex >= AllRows.Count)
            return this;
        var headers = AllRows[headerRowIndex];
        var data = AllRows.Skip(headerRowIndex + 1)
            .Where(r => r.Any(c => !string.IsNullOrWhiteSpace(c)))
            .ToList();
        return new CsvTable
        {
            Delimiter = Delimiter,
            EncodingName = EncodingName,
            HeaderRowIndex = headerRowIndex,
            Headers = headers,
            DataRows = data,
            AllRows = AllRows
        };
    }
}
