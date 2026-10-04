using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Import;

public class AnalyzeCsvImportUseCase(
    CsvImportOrchestrator orchestrator,
    PrepareCsvImportUseCase prepare)
{
    public async Task<ImportPreviewResult> ExecuteAsync(
        Stream csvStream,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        var prepared = await prepare.ExecuteAsync(csvStream, accountId, cancellationToken).ConfigureAwait(false);
        if (prepared.ErrorMessage is not null)
            return new ImportPreviewResult { ErrorMessage = prepared.ErrorMessage };
        if (prepared.NeedsMapping)
            return new ImportPreviewResult { ErrorMessage = ImportMessageKeys.CsvNeedsMapping };
        return prepared.Preview!;
    }

    public Task<ImportPreviewResult> ExecuteAsync(
        CsvTable table,
        CsvImportProfile profile,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(table);
        ArgumentNullException.ThrowIfNull(profile);
        var dtos = CsvMappingApplier.Apply(table, profile);
        return orchestrator.AnalyzeDtosAsync(dtos, accountId, cancellationToken);
    }
}
