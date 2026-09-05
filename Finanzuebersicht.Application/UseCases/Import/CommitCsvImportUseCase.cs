using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Import;

public class CommitCsvImportUseCase(CsvImportOrchestrator orchestrator)
{
    private readonly CsvImportOrchestrator _orchestrator = orchestrator;

    public Task<ImportResult> ExecuteAsync(
        ImportPreviewResult preview,
        IEnumerable<string>? selectedRowIds = null,
        CancellationToken cancellationToken = default)
        => _orchestrator.CommitImportAsync(preview, selectedRowIds, cancellationToken);
}
