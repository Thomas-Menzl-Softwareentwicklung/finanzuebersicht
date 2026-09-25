using Finanzuebersicht.Core.Services;
using Finanzuebersicht.Models;

namespace Finanzuebersicht.Presentation.Services;

public class ImportSessionStore : IImportSessionStore
{
    private ImportPreviewResult? _activeSession;
    private CsvTable? _table;
    private CsvImportProfile? _profile;
    private string? _accountId;

    public void SetTable(CsvTable table, string? accountId)
    {
        _table = table;
        _accountId = accountId;
    }

    public void SetActiveSession(ImportPreviewResult preview, CsvImportProfile? profile = null)
    {
        _activeSession = preview;
        if (profile is not null)
            _profile = profile;
    }

    public ImportPreviewResult? GetActiveSession() => _activeSession;

    public CsvTable? GetTable() => _table;

    public CsvImportProfile? GetProfile() => _profile;

    public string? GetAccountId() => _accountId;

    public bool CanRemap => _table is not null;

    public void Clear()
    {
        _activeSession = null;
        _table = null;
        _profile = null;
        _accountId = null;
    }
}
