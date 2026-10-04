using Finanzuebersicht.Core.Services;

namespace Finanzuebersicht.Application.UseCases.Import;

public class PrepareCsvImportUseCase(
    ICsvImportProfileStore profileStore,
    CsvImportOrchestrator orchestrator)
{
    public async Task<CsvImportPrepareResult> ExecuteAsync(
        Stream stream,
        string? accountId = null,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] buffer;
        try
        {
            using var ms = new MemoryStream();
            await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
            buffer = ms.ToArray();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch
        {
            return new CsvImportPrepareResult { ErrorMessage = ImportMessageKeys.FileReadFailed };
        }

        if (!CsvTableReader.TryRead(buffer, out var table, out var errorKey))
            return new CsvImportPrepareResult { ErrorMessage = errorKey };

        var userProfiles = await profileStore.GetUserProfilesAsync().ConfigureAwait(false);
        var profile = CsvImportProfileMatcher.Find(table!, userProfiles);
        if (profile is null)
        {
            foreach (var candidate in userProfiles)
            {
                var rebuilt = table!.WithHeaderRow(candidate.HeaderRowIndex);
                if (!CsvImportFingerprint.Matches(rebuilt, candidate))
                    continue;

                profile = candidate;
                table = rebuilt;
                break;
            }
        }

        if (profile is null)
            return new CsvImportPrepareResult { Table = table };

        cancellationToken.ThrowIfCancellationRequested();

        var dtos = CsvMappingApplier.Apply(table!, profile);
        var preview = await orchestrator.AnalyzeDtosAsync(dtos, accountId, cancellationToken).ConfigureAwait(false);
        return new CsvImportPrepareResult
        {
            Table = table,
            Profile = profile,
            Preview = preview
        };
    }
}
