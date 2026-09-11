namespace Finanzuebersicht.Core.Services;

public enum CsvDecimalStyle
{
    Comma,
    Point
}

public record CsvColumnMapping
{
    public string? Date { get; init; }
    public string? Amount { get; init; }
    public string? Title { get; init; }
    public string? Purpose { get; init; }
    public string? AmountSign { get; init; }
    public string? Iban { get; init; }
}

public class CsvImportProfile
{
    public string Id { get; init; } = "";
    public string Name { get; init; } = "";
    public bool IsBuiltIn { get; init; }
    public char Delimiter { get; init; }
    public IReadOnlyList<string> Headers { get; init; } = [];
    public int HeaderRowIndex { get; init; }
    public CsvColumnMapping Columns { get; init; } = new();
    public string DateFormat { get; init; } = "dd.MM.yyyy";
    public CsvDecimalStyle DecimalStyle { get; init; } = CsvDecimalStyle.Comma;

    public bool IsComplete =>
        Columns.Date is not null
        && Columns.Amount is not null
        && (Columns.Title is not null || Columns.Purpose is not null);
}
