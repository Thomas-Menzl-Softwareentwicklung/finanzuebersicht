using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Import;

public sealed class CsvImportPrepareResult
{
    public CsvTable? Table { get; init; }
    public CsvImportProfile? Profile { get; init; }
    public ImportPreviewResult? Preview { get; init; }
    public string? ErrorMessage { get; init; }
    public bool NeedsMapping => ErrorMessage is null && Preview is null && Table is not null;
}
