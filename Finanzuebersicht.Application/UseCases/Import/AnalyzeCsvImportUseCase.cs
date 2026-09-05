using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Import;

public class AnalyzeCsvImportUseCase(CsvImportOrchestrator orchestrator)
{
    private readonly CsvImportOrchestrator _orchestrator = orchestrator;

    public Task<ImportPreviewResult> ExecuteAsync(
        Stream csvStream,
        string? accountId = null,
        CancellationToken cancellationToken = default)
        => _orchestrator.AnalyzeCsvAsync(csvStream, accountId, cancellationToken);
}
