# Generic CSV Import (Column Mapping) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the DKB-only `IStatementParser` path with one CSV pipeline: table reader → profile match (built-in DKB or saved mapping) → existing import preview/commit.

**Architecture:** `CsvTableReader` turns bytes into a headered table. `CsvImportProfile` maps header names onto date/amount/title/purpose/sign/IBAN. Built-in DKB plus user JSON (`csv-import-profiles.json`) share one matcher. `CsvMappingApplier` emits today’s `TransactionDto`; Analyze/Commit stay. Unknown fingerprints open `ImportMappingPage`; preview can remap.

**Tech Stack:** .NET 10, xUnit, NSubstitute, MAUI `SelectionField`, Resx DE/EN, JSON stores (`JsonDataStoreBase`).

**Spec:** `docs/superpowers/specs/2026-09-11-generic-csv-import-design.md` (#360)

## Global Constraints

- Pro-Gate `AppFeature.CsvImport` unchanged; Direct + Store.
- No native `Picker` on Mac Catalyst — `SelectionField` + `SelectionPopup` only.
- No UI strings in Application; keys via `ImportMessageKeys` / `ResourceKeys` + both Resx files.
- CAMT (#361), Open Banking (#245), Soll+Haben columns, Settings profile list, US `MM/dd/yyyy` auto-detect are out of scope.
- Fingerprint = delimiter + headers trimmed, `ToLowerInvariant()`, order significant.
- User profile with the same fingerprint wins over built-in DKB; never write the built-in to disk.
- ZIP backup includes `csv-import-profiles.json`; missing in old archives = empty user profiles. Not CloudKit.

## File map

| File | Responsibility |
|------|----------------|
| `Finanzuebersicht.Core/Services/CsvTable.cs` | Headered table + header-row rebuild |
| `Finanzuebersicht.Core/Services/CsvTableReader.cs` | Encoding, delimiter, quoted CSV, header heuristic |
| `Finanzuebersicht.Core/Services/CsvImportProfile.cs` | Profile + column mapping + decimal style |
| `Finanzuebersicht.Core/Services/CsvImportFingerprint.cs` | Normalize / compute fingerprint |
| `Finanzuebersicht.Core/Services/DkbCsvImportProfile.cs` | Built-in DKB mapping (exact export headers) |
| `Finanzuebersicht.Core/Services/CsvMappingGuesser.cs` | Header → field guesses |
| `Finanzuebersicht.Core/Services/CsvFormatDetector.cs` | Date format + decimal style from samples |
| `Finanzuebersicht.Core/Services/CsvMappingApplier.cs` | Profile + table → `TransactionDto` |
| `Finanzuebersicht.Core/Services/CsvImportProfileMatcher.cs` | User profiles then DKB |
| `Finanzuebersicht.Core/Services/ICsvImportProfileStore.cs` | User profile persistence |
| `Finanzuebersicht.Infrastructure/Services/FileCsvImportProfileStore.cs` | `csv-import-profiles.json` |
| `Finanzuebersicht.Application/UseCases/Import/PrepareCsvImportUseCase.cs` | Stream → table + match + maybe preview |
| `Finanzuebersicht.Application/UseCases/Import/CsvImportPrepareResult.cs` | NeedsMapping vs preview vs error |
| `Finanzuebersicht.Application/UseCases/Import/CsvImportOrchestrator.cs` | Drop `IStatementParser`; analyze DTOs only |
| `Finanzuebersicht.Presentation/ViewModels/ImportMappingViewModel.cs` | Mapping UI |
| `Finanzuebersicht/Views/ImportMappingPage.xaml` | Mapping page |
| `Finanzuebersicht.Presentation/Services/ImportSessionStore.cs` | Table + profile + preview |

Delete after golden tests pass: `DkbCsvParser.cs`, production `IStatementParser` registration.

---

### Task 1: CsvTableReader

**Files:**
- Create: `Finanzuebersicht.Core/Services/CsvTable.cs`
- Create: `Finanzuebersicht.Core/Services/CsvTableReader.cs`
- Create: `Finanzuebersicht.Tests/Core/Services/CsvTableReaderTests.cs`
- Test fixtures already at `Finanzuebersicht.Tests/Services/test_dkb_sample.csv`, `test_dkb_multiline.csv`

**Interfaces:**
- Consumes: none
- Produces: `CsvTableReader.TryRead(byte[] bytes, out CsvTable? table, out string? errorKey)` → `bool`; `CsvTable.WithHeaderRow(int headerRowIndex)` → `CsvTable`; `CsvTable.Headers`, `DataRows`, `Delimiter`, `HeaderRowIndex`, `AllRows`

- [ ] **Step 1: Write the failing tests**

```csharp
using System.Text;
using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvTableReaderTests
{
    private static string RepoRoot =>
        Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", ".."));

    private static byte[] ReadFixture(string name) =>
        File.ReadAllBytes(Path.Combine(RepoRoot, "Finanzuebersicht.Tests", "Services", name));

    [Fact]
    public void TryRead_DkbSample_FindsHeaderAfterPreamble()
    {
        Assert.True(CsvTableReader.TryRead(ReadFixture("test_dkb_sample.csv"), out var table, out var error));
        Assert.Null(error);
        Assert.Equal(';', table!.Delimiter);
        Assert.Equal("Buchungsdatum", table.Headers[0]);
        Assert.Equal(4, table.DataRows.Count);
        Assert.Contains(table.Headers, h => h.StartsWith("Betrag", StringComparison.Ordinal));
    }

    [Fact]
    public void TryRead_MultilineQuotedField_KeepsNewlinesAndSemicolon()
    {
        Assert.True(CsvTableReader.TryRead(ReadFixture("test_dkb_multiline.csv"), out var table, out _));
        Assert.Single(table!.DataRows);
        var purpose = table.DataRows[0][5];
        Assert.Contains("Abonnement Linie1", purpose);
        Assert.Contains("Abonnement Linie2", purpose);
        Assert.Contains("Zusatzinfo", purpose);
    }

    [Fact]
    public void TryRead_CommaDelimited_DetectsComma()
    {
        var csv = "Date,Amount,Text\n2026-03-01,12.50,Coffee\n";
        Assert.True(CsvTableReader.TryRead(Encoding.UTF8.GetBytes(csv), out var table, out _));
        Assert.Equal(',', table!.Delimiter);
        Assert.Equal(["Date", "Amount", "Text"], table.Headers);
        Assert.Equal("12.50", table.DataRows[0][1]);
    }

    [Fact]
    public void TryRead_TabDelimited_DetectsTab()
    {
        var csv = "Date\tAmount\n01.03.26\t10,00\n";
        Assert.True(CsvTableReader.TryRead(Encoding.UTF8.GetBytes(csv), out var table, out _));
        Assert.Equal('\t', table!.Delimiter);
    }

    [Fact]
    public void TryRead_Windows1252Umlauts_DecodesWhenNotUtf8()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var latin1 = Encoding.GetEncoding(1252);
        var csv = "Datum;Betrag;Text\n01.03.26;-10,00;Gebühr\n";
        Assert.True(CsvTableReader.TryRead(latin1.GetBytes(csv), out var table, out _));
        Assert.Equal("Gebühr", table!.DataRows[0][2]);
    }

    [Fact]
    public void WithHeaderRow_OverridesHeuristic()
    {
        var csv = "skip-me;x;y\nDatum;Betrag;Text\n01.03.26;1,00;A\n";
        Assert.True(CsvTableReader.TryRead(Encoding.UTF8.GetBytes(csv), out var table, out _));
        var overridden = table!.WithHeaderRow(1);
        Assert.Equal("Datum", overridden.Headers[0]);
        Assert.Single(overridden.DataRows);
    }

    [Fact]
    public void TryRead_EmptyBytes_Fails()
    {
        Assert.False(CsvTableReader.TryRead([], out var table, out var error));
        Assert.Null(table);
        Assert.Equal(ImportMessageKeys.CsvNotTabular, error);
    }
}
```

Add to `ImportMessageKeys.cs`:

```csharp
public const string CsvNotTabular = "Msg_ImportCsvNotTabular";
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvTableReaderTests" --nologo`

Expected: FAIL (type `CsvTableReader` not found)

- [ ] **Step 3: Implement CsvTable + CsvTableReader**

`CsvTable.cs`:

```csharp
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
```

`CsvTableReader.cs` (put encoding provider register in a static ctor). Behavior:

1. Reject null/empty bytes → `CsvNotTabular`.
2. Decode: UTF-8 BOM → UTF-8; else try `new UTF8Encoding(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true).GetString(bytes)`; on `DecoderFallbackException` use `Encoding.GetEncoding(1252)` after `Encoding.RegisterProvider(CodePagesEncodingProvider.Instance)` in a static ctor. If 1252 is unavailable, add NuGet `System.Text.Encoding.CodePages` to `Finanzuebersicht.Core.csproj`.
3. Parse records with the same quote rules as today’s `DkbCsvParser.ParseCsv` (copy that loop; delimiter is still unknown — first pass: split candidate lines on `;`, `,`, `\t` and pick the delimiter that yields the highest consistent column count ≥ 2 on the first 15 non-empty lines).
4. Header heuristic: first row with ≥ 3 non-empty cells where a majority are non-numeric **or** any cell contains (case-insensitive) `datum`, `date`, `buchung`, `betrag`, `amount`, `verwendung`, `iban`, `umsatz`. DKB preamble must lose to the `Buchungsdatum` row.
5. `DataRows` = non-empty rows after the header.
6. If no header found, use row 0 if it has ≥ 2 cells; else fail `CsvNotTabular`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvTableReaderTests" --nologo`

Expected: PASS (all 7)

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Services/CsvTable.cs \
  Finanzuebersicht.Core/Services/CsvTableReader.cs \
  Finanzuebersicht.Core/Services/ImportMessageKeys.cs \
  Finanzuebersicht.Tests/Core/Services/CsvTableReaderTests.cs
git commit -m "$(cat <<'EOF'
feat(import): add CSV table reader with delimiter and header detection

EOF
)"
```

---

### Task 2: Profile model, fingerprint, built-in DKB

**Files:**
- Create: `Finanzuebersicht.Core/Services/CsvImportProfile.cs`
- Create: `Finanzuebersicht.Core/Services/CsvImportFingerprint.cs`
- Create: `Finanzuebersicht.Core/Services/DkbCsvImportProfile.cs`
- Create: `Finanzuebersicht.Tests/Core/Services/CsvImportFingerprintTests.cs`

**Interfaces:**
- Consumes: `CsvTable.Headers`, `CsvTable.Delimiter`
- Produces: `CsvImportProfile` (`Id`, `Name`, `IsBuiltIn`, `Delimiter`, `Headers`, `HeaderRowIndex`, `Columns`, `DateFormat`, `DecimalStyle`); `CsvColumnMapping` **record** with nullable header names `Date`, `Amount`, `Title`, `Purpose`, `AmountSign`, `Iban`; `CsvDecimalStyle { Comma, Point }`; `CsvImportFingerprint.Normalize`, `Compute`, `Matches(CsvTable, CsvImportProfile)`; `DkbCsvImportProfile.Instance` with id `builtin-dkb`

- [ ] **Step 1: Write the failing tests**

```csharp
using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvImportFingerprintTests
{
    [Fact]
    public void Matches_DkbHeaders_AgainstBuiltIn()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 3,
            Headers = DkbCsvImportProfile.Instance.Headers,
            DataRows = [],
            AllRows = []
        };
        Assert.True(CsvImportFingerprint.Matches(table, DkbCsvImportProfile.Instance));
    }

    [Fact]
    public void Matches_IgnoresHeaderCaseAndTrim()
    {
        var dkb = DkbCsvImportProfile.Instance;
        var headers = dkb.Headers.Select(h => " " + h.ToUpperInvariant() + " ").ToList();
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = headers,
            DataRows = [],
            AllRows = []
        };
        Assert.True(CsvImportFingerprint.Matches(table, dkb));
    }

    [Fact]
    public void Matches_ExtraColumn_IsDifferentPattern()
    {
        var headers = DkbCsvImportProfile.Instance.Headers.Append("Extra").ToList();
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = headers,
            DataRows = [],
            AllRows = []
        };
        Assert.False(CsvImportFingerprint.Matches(table, DkbCsvImportProfile.Instance));
    }

    [Fact]
    public void DkbProfile_MapsRequiredExportHeaders()
    {
        var p = DkbCsvImportProfile.Instance;
        Assert.Equal("builtin-dkb", p.Id);
        Assert.True(p.IsBuiltIn);
        Assert.Equal("Buchungsdatum", p.Columns.Date);
        Assert.Equal("Betrag (€)", p.Columns.Amount);
        Assert.Equal("Zahlungsempfänger*in", p.Columns.Title);
        Assert.Equal("Verwendungszweck", p.Columns.Purpose);
        Assert.Equal("Umsatztyp", p.Columns.AmountSign);
        Assert.Equal("IBAN", p.Columns.Iban);
        Assert.Equal("dd.MM.yy", p.DateFormat);
        Assert.Equal(CsvDecimalStyle.Comma, p.DecimalStyle);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvImportFingerprintTests" --nologo`

Expected: FAIL (types missing)

- [ ] **Step 3: Implement profile types**

`CsvImportFingerprint.Normalize(string header)` = `header.Trim().ToLowerInvariant()`.

`Compute(char delimiter, IReadOnlyList<string> headers)` = `delimiter + "\n" + string.Join('\n', headers.Select(Normalize))`.

`Matches` compares `table.Delimiter` and `Compute` vs profile’s stored headers/delimiter.

`DkbCsvImportProfile.Instance` headers **in this exact order** (DKB export):

`Buchungsdatum`, `Wertstellung`, `Status`, `Zahlungspflichtige*r`, `Zahlungsempfänger*in`, `Verwendungszweck`, `Umsatztyp`, `IBAN`, `Betrag (€)`, `Gläubiger-ID`, `Mandatsreferenz`, `Kundenreferenz`

`CsvImportProfile` defaults: `IsBuiltIn = false`, `HeaderRowIndex = 0`, `Headers = []`, `Name = ""`, `DateFormat = "dd.MM.yyyy"`, `DecimalStyle = Comma`, `Columns = new()`. `IsComplete` → Date and Amount set AND (Title or Purpose set).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvImportFingerprintTests" --nologo`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Services/CsvImportProfile.cs \
  Finanzuebersicht.Core/Services/CsvImportFingerprint.cs \
  Finanzuebersicht.Core/Services/DkbCsvImportProfile.cs \
  Finanzuebersicht.Tests/Core/Services/CsvImportFingerprintTests.cs
git commit -m "$(cat <<'EOF'
feat(import): add CSV profile fingerprint and built-in DKB mapping

EOF
)"
```

---

### Task 3: Column guesser and format detector

**Files:**
- Create: `Finanzuebersicht.Core/Services/CsvMappingGuesser.cs`
- Create: `Finanzuebersicht.Core/Services/CsvFormatDetector.cs`
- Create: `Finanzuebersicht.Tests/Core/Services/CsvMappingGuesserTests.cs`

**Interfaces:**
- Consumes: `CsvTable`, `CsvColumnMapping`, `CsvDecimalStyle`
- Produces: `CsvMappingGuesser.Guess(CsvTable) → CsvColumnMapping`; `CsvFormatDetector.DetectDateFormat(IEnumerable<string> samples) → string` (one of `dd.MM.yy`, `dd.MM.yyyy`, `yyyy-MM-dd`, `dd/MM/yyyy`, `d.M.yyyy`; default `dd.MM.yyyy` if none win); `CsvFormatDetector.DetectDecimalStyle(IEnumerable<string> samples) → CsvDecimalStyle`

Guessing rules (longest token first; normalize header: trim, case-fold, strip `*` and `(€)` / `€`; match equals token or token + suffix `in` / `/in`). Never map `Umsatz` onto `Umsatztyp` or `Typ` onto `Umsatztyp`. Prefer Date tokens over `Wertstellung`/`Valuta`. Amount tokens: `Betrag`, `Amount`, `Umsatz` but not `Kontostand`. Title: `Zahlungsempfänger`, `Empfänger`, `Payee`, `Auftraggeber`. Purpose: `Verwendungszweck`, `Buchungstext`, `Beschreibung`, `Text`, `Purpose`. AmountSign: `Umsatztyp`, `Soll/Haben`, `S/H`, `Debit/Credit`, `Typ`. IBAN: `IBAN`.

- [ ] **Step 1: Write the failing tests**

```csharp
using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvMappingGuesserTests
{
    [Fact]
    public void Guess_DkbHeaders_FillsAllTargets()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = DkbCsvImportProfile.Instance.Headers,
            DataRows = [],
            AllRows = []
        };
        var g = CsvMappingGuesser.Guess(table);
        Assert.Equal("Buchungsdatum", g.Date);
        Assert.Equal("Betrag (€)", g.Amount);
        Assert.Equal("Zahlungsempfänger*in", g.Title);
        Assert.Equal("Verwendungszweck", g.Purpose);
        Assert.Equal("Umsatztyp", g.AmountSign);
        Assert.Equal("IBAN", g.Iban);
    }

    [Fact]
    public void Guess_DoesNotTreatUmsatztypAsAmount()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Umsatztyp", "Betrag"],
            DataRows = [],
            AllRows = []
        };
        var g = CsvMappingGuesser.Guess(table);
        Assert.Equal("Betrag", g.Amount);
        Assert.Equal("Umsatztyp", g.AmountSign);
    }

    [Fact]
    public void DetectDateFormat_IsoAndGerman()
    {
        Assert.Equal("yyyy-MM-dd", CsvFormatDetector.DetectDateFormat(["2026-03-01", "2026-03-05"]));
        Assert.Equal("dd.MM.yy", CsvFormatDetector.DetectDateFormat(["01.03.26", "05.03.26"]));
        Assert.Equal("dd.MM.yyyy", CsvFormatDetector.DetectDateFormat(["01.03.2026", "05.03.2026"]));
    }

    [Fact]
    public void DetectDecimalStyle_CommaVsPoint()
    {
        Assert.Equal(CsvDecimalStyle.Comma, CsvFormatDetector.DetectDecimalStyle(["1.234,56", "-45,20"]));
        Assert.Equal(CsvDecimalStyle.Point, CsvFormatDetector.DetectDecimalStyle(["1234.56", "-45.20"]));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvMappingGuesserTests" --nologo`

Expected: FAIL

- [ ] **Step 3: Implement guesser + detector**

Date detection: for each candidate format, count samples that `DateTime.TryParseExact` with `CultureInfo.InvariantCulture` (German formats also try `de-DE`). Pick the format with the highest count if it is a strict majority of non-empty samples; do not consider `MM/dd/yyyy`.

Decimal: last separator that is followed by 1–2 digits is the decimal mark. If that mark is `,` → `Comma`, if `.` → `Point`. If mixed, majority wins. Default `Comma`.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvMappingGuesserTests" --nologo`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Services/CsvMappingGuesser.cs \
  Finanzuebersicht.Core/Services/CsvFormatDetector.cs \
  Finanzuebersicht.Tests/Core/Services/CsvMappingGuesserTests.cs
git commit -m "$(cat <<'EOF'
feat(import): guess CSV columns and date/number formats from headers

EOF
)"
```

---

### Task 4: CsvMappingApplier

**Files:**
- Create: `Finanzuebersicht.Core/Services/CsvMappingApplier.cs`
- Create: `Finanzuebersicht.Tests/Core/Services/CsvMappingApplierTests.cs`

**Interfaces:**
- Consumes: `CsvTable`, `CsvImportProfile`, `TransactionDto`
- Produces: `CsvMappingApplier.Apply(CsvTable table, CsvImportProfile profile) → IReadOnlyList<TransactionDto>` (skip rows that cannot parse date or amount; do not throw)

Sign tokens (trim, ignore case): income `Eingang`, `Haben`, `Credit`, `Einnahme`, `+`; expense `Ausgang`, `Soll`, `Debit`, `Ausgabe`, `-`. If AmountSign mapped and token known, set sign of `Betrag` (absolute parsed amount × sign). Unknown token → keep parsed amount sign. No AmountSign → parsed sign.

Title column → `Zahlungsempfaenger`. Purpose → `Verwendungszweck`. IBAN → `IBAN`. `Wertstellung` = `Buchungsdatum`.

Amount parse: strip `€` and spaces; `Comma` style uses `de-DE`; `Point` style uses `en-US` (`NumberStyles.Number | AllowLeadingSign`).

- [ ] **Step 1: Write the failing tests**

```csharp
using Finanzuebersicht.Core.Services;
using Xunit;

namespace Finanzuebersicht.Tests.Core.Services;

public class CsvMappingApplierTests
{
    [Fact]
    public void Apply_DkbProfile_OnSampleFixture_MatchesGoldenAmounts()
    {
        var bytes = File.ReadAllBytes(Path.Combine(
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..")),
            "Finanzuebersicht.Tests", "Services", "test_dkb_sample.csv"));
        Assert.True(CsvTableReader.TryRead(bytes, out var table, out _));
        var dtos = CsvMappingApplier.Apply(table!, DkbCsvImportProfile.Instance);
        Assert.Equal(4, dtos.Count);
        Assert.Equal(2500.00m, dtos.Single(d => d.Zahlungsempfaenger.Contains("Muster")).Betrag);
        Assert.Equal(-7.99m, dtos.Single(d => d.Zahlungsempfaenger.Contains("Streaming")).Betrag);
        Assert.Equal("Gehaltszahlung März", dtos.Single(d => d.Betrag == 2500.00m).Verwendungszweck);
    }

    [Fact]
    public void Apply_TypeColumn_OverridesUnsignedAmount()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Typ", "Text"],
            DataRows =
            [
                ["01.03.26", "10,00", "Ausgang", "Shop"],
                ["02.03.26", "20,00", "Eingang", "Pay"]
            ],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping
            {
                Date = "Datum",
                Amount = "Betrag",
                AmountSign = "Typ",
                Title = "Text"
            }
        };
        var dtos = CsvMappingApplier.Apply(table, profile);
        Assert.Equal(-10.00m, dtos[0].Betrag);
        Assert.Equal(20.00m, dtos[1].Betrag);
    }

    [Fact]
    public void Apply_UnknownType_FallsBackToAmountSign()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Typ", "Text"],
            DataRows = [["01.03.26", "-5,00", "???", "X"]],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping
            {
                Date = "Datum", Amount = "Betrag", AmountSign = "Typ", Title = "Text"
            }
        };
        Assert.Equal(-5.00m, CsvMappingApplier.Apply(table, profile)[0].Betrag);
    }

    [Fact]
    public void Apply_IsoDateAndPointDecimal()
    {
        var table = new CsvTable
        {
            Delimiter = ',',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Date", "Amount", "Text"],
            DataRows = [["2026-03-01", "12.50", "Coffee"]],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ',',
            Headers = table.Headers,
            DateFormat = "yyyy-MM-dd",
            DecimalStyle = CsvDecimalStyle.Point,
            Columns = new CsvColumnMapping { Date = "Date", Amount = "Amount", Title = "Text" }
        };
        var dto = Assert.Single(CsvMappingApplier.Apply(table, profile));
        Assert.Equal(new DateTime(2026, 3, 1), dto.Buchungsdatum.Date);
        Assert.Equal(12.50m, dto.Betrag);
    }

    [Fact]
    public void Apply_SkipsUnparsableDate()
    {
        var table = new CsvTable
        {
            Delimiter = ';',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Datum", "Betrag", "Text"],
            DataRows =
            [
                ["nope", "1,00", "A"],
                ["01.03.26", "2,00", "B"]
            ],
            AllRows = []
        };
        var profile = new CsvImportProfile
        {
            Id = "t",
            Name = "t",
            Delimiter = ';',
            Headers = table.Headers,
            DateFormat = "dd.MM.yy",
            DecimalStyle = CsvDecimalStyle.Comma,
            Columns = new CsvColumnMapping { Date = "Datum", Amount = "Betrag", Title = "Text" }
        };
        Assert.Equal("B", Assert.Single(CsvMappingApplier.Apply(table, profile)).Zahlungsempfaenger);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvMappingApplierTests" --nologo`

Expected: FAIL

- [ ] **Step 3: Implement applier**

Resolve column index by exact header string first, then `CsvImportFingerprint.Normalize` equality. Missing mapped header → skip all rows (return empty), do not throw.

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvMappingApplierTests" --nologo`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Services/CsvMappingApplier.cs \
  Finanzuebersicht.Tests/Core/Services/CsvMappingApplierTests.cs
git commit -m "$(cat <<'EOF'
feat(import): apply CSV column mapping to transaction DTOs

EOF
)"
```

---

### Task 5: Profile store and matcher

**Files:**
- Create: `Finanzuebersicht.Core/Services/ICsvImportProfileStore.cs`
- Create: `Finanzuebersicht.Core/Services/CsvImportProfileMatcher.cs`
- Create: `Finanzuebersicht.Infrastructure/Services/FileCsvImportProfileStore.cs`
- Create: `Finanzuebersicht.Tests/Core/Services/CsvImportProfileMatcherTests.cs`
- Create: `Finanzuebersicht.Tests/Infrastructure/FileCsvImportProfileStoreTests.cs`
- Modify: `Finanzuebersicht.Core/Constants/DataFileNames.cs` — add `CsvImportProfiles = "csv-import-profiles.json"`
- Modify: `Finanzuebersicht.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` — register store; **remove** `IStatementParser` / `DkbCsvParser` in Task 8, not yet

**Interfaces:**
- Consumes: `CsvTable`, `CsvImportProfile`, `DkbCsvImportProfile.Instance`
- Produces: `ICsvImportProfileStore.GetUserProfilesAsync()`, `UpsertAsync(CsvImportProfile)`, `ReplaceAllAsync(IEnumerable<CsvImportProfile>)`; `CsvImportProfileMatcher.Find(CsvTable, IReadOnlyList<CsvImportProfile> userProfiles)` → user match else DKB else `null`

Upsert: if `IsBuiltIn` is true, clone to a new user profile (`IsBuiltIn = false`, new Guid id, same fingerprint/mapping) before save — store never persists `builtin-dkb`. Matcher: first user profile where `CsvImportFingerprint.Matches`, else DKB if matches, else null.

- [ ] **Step 1: Write the failing tests**

```csharp
public class CsvImportProfileMatcherTests
{
    private static CsvTable TableFor(CsvImportProfile profile) => new()
    {
        Delimiter = profile.Delimiter,
        EncodingName = "utf-8",
        HeaderRowIndex = 0,
        Headers = profile.Headers,
        DataRows = [],
        AllRows = []
    };

    [Fact]
    public void Find_EmptyUsers_DkbHeaders_ReturnsBuiltIn()
    {
        var found = CsvImportProfileMatcher.Find(TableFor(DkbCsvImportProfile.Instance), []);
        Assert.Same(DkbCsvImportProfile.Instance, found);
    }

    [Fact]
    public void Find_UserWithSameFingerprint_WinsOverDkb()
    {
        var user = new CsvImportProfile
        {
            Id = "user-1",
            Name = "custom",
            Delimiter = ';',
            Headers = DkbCsvImportProfile.Instance.Headers,
            Columns = DkbCsvImportProfile.Instance.Columns with { Title = "Zahlungspflichtige*r" }
        };
        var found = CsvImportProfileMatcher.Find(TableFor(DkbCsvImportProfile.Instance), [user]);
        Assert.Equal("user-1", found!.Id);
        Assert.Equal("Zahlungspflichtige*r", found.Columns.Title);
    }

    [Fact]
    public void Find_UnknownHeaders_ReturnsNull()
    {
        var table = new CsvTable
        {
            Delimiter = ',',
            EncodingName = "utf-8",
            HeaderRowIndex = 0,
            Headers = ["Date", "Amount", "Text"],
            DataRows = [],
            AllRows = []
        };
        Assert.Null(CsvImportProfileMatcher.Find(table, []));
    }
}
```

`CsvColumnMapping` must be a `record` so `with` works. Store test: temp directory, `UpsertAsync` a profile, `GetUserProfilesAsync` returns it with same `Id` and `Columns.Date`; `ReplaceAllAsync([])` then get returns empty; upserting `IsBuiltIn = true` persists `IsBuiltIn = false` and a new id (not `builtin-dkb`).

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~CsvImportProfileMatcherTests|FullyQualifiedName~FileCsvImportProfileStoreTests" --nologo`

Expected: FAIL

- [ ] **Step 3: Implement matcher + FileCsvImportProfileStore**

Subclass `JsonDataStoreBase`. File path `Path.Combine(DataDir, DataFileNames.CsvImportProfiles)`. Register:

```csharp
services.AddSingleton<ICsvImportProfileStore>(sp =>
    new FileCsvImportProfileStore(
        GetDataDir(sp),
        sp.GetService<ILogger<FileCsvImportProfileStore>>()));
```

Do not add the store to `LocalDataService`.

- [ ] **Step 4: Run tests to verify they pass**

Run: same filter as step 2. Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Services/ICsvImportProfileStore.cs \
  Finanzuebersicht.Core/Services/CsvImportProfileMatcher.cs \
  Finanzuebersicht.Infrastructure/Services/FileCsvImportProfileStore.cs \
  Finanzuebersicht.Core/Constants/DataFileNames.cs \
  Finanzuebersicht.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs \
  Finanzuebersicht.Tests/Core/Services/CsvImportProfileMatcherTests.cs \
  Finanzuebersicht.Tests/Infrastructure/FileCsvImportProfileStoreTests.cs
git commit -m "$(cat <<'EOF'
feat(import): persist CSV mapping profiles and match by fingerprint

EOF
)"
```

---

### Task 6: Prepare use case and drop parser zoo from orchestrator

**Files:**
- Create: `Finanzuebersicht.Application/UseCases/Import/CsvImportPrepareResult.cs`
- Create: `Finanzuebersicht.Application/UseCases/Import/PrepareCsvImportUseCase.cs`
- Modify: `Finanzuebersicht.Application/UseCases/Import/CsvImportOrchestrator.cs` — constructor no longer takes `IEnumerable<IStatementParser>`; add `AnalyzeDtosAsync(IReadOnlyList<TransactionDto> dtos, string? accountId, CancellationToken)`; `AnalyzeCsvAsync(Stream)` becomes: read bytes → reader → match → apply → `AnalyzeDtosAsync`. If no profile: return `ImportPreviewResult` with a new flag (do **not** use this for UI — UI uses Prepare). Prefer keeping AnalyzeCsvAsync only if still needed; Presentation should call Prepare.
- Modify: `Finanzuebersicht.Application/UseCases/Import/AnalyzeCsvImportUseCase.cs` — add `ExecuteAsync(CsvTable table, CsvImportProfile profile, string? accountId, CancellationToken)` applying then analyzing
- Modify: `Finanzuebersicht.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs` — register `PrepareCsvImportUseCase`
- Modify: `Finanzuebersicht.Tests/Application/UseCases/Import/CsvImportUseCaseTests.cs` — stop faking `IStatementParser`; call `AnalyzeDtosAsync` / commit via DTOs
- Create: `Finanzuebersicht.Tests/Application/UseCases/Import/PrepareCsvImportUseCaseTests.cs`

**Interfaces:**
- Consumes: `CsvTableReader`, `ICsvImportProfileStore`, `CsvImportProfileMatcher`, `CsvMappingApplier`, `CsvImportOrchestrator.AnalyzeDtosAsync`
- Produces:

```csharp
public sealed class CsvImportPrepareResult
{
    public CsvTable? Table { get; init; }
    public CsvImportProfile? Profile { get; init; }
    public ImportPreviewResult? Preview { get; init; }
    public string? ErrorMessage { get; init; }
    public bool NeedsMapping => ErrorMessage is null && Preview is null && Table is not null;
}
```

`PrepareCsvImportUseCase.ExecuteAsync(Stream stream, string? accountId, CancellationToken)`:

1. Copy stream to bytes; on IO error → `ErrorMessage = ImportMessageKeys.FileReadFailed`.
2. `CsvTableReader.TryRead` fail → that error key.
3. `userProfiles = await store.GetUserProfilesAsync`.
4. `profile = CsvImportProfileMatcher.Find(table, userProfiles)`.
5. If null → `{ Table = table, NeedsMapping implied }`.
6. Else apply + `AnalyzeDtosAsync` → `{ Table, Profile, Preview }`.

Honor cancellation at the start and before analyze.

Remove tests `ImportFromCsv_NoParserMatches` and `ImportFromCsv_ParserThrows`. Change remaining orchestrator tests to build without parsers:

```csharp
new CsvImportOrchestrator(repo, logger, catRepo, categorizationService, accountRepository: null, uncategorized)
```

`ImportFromCsvAsync`: prepare is not inside orchestrator. Delete `ImportFromCsvAsync` **or** keep it only as analyze-dtos+commit for the compatibility test by passing DTOs. Replace `ImportFromCsv_CompatibilityWrapper_*` with `AnalyzeDtos` + `CommitImportAsync`.

- [ ] **Step 1: Write failing Prepare tests + rewrite orchestrator tests to AnalyzeDtos**

`PrepareCsvImportUseCaseTests.cs`:

```csharp
public sealed class InMemoryCsvImportProfileStore : ICsvImportProfileStore
{
    private readonly List<CsvImportProfile> _items = [];
    public Task<IReadOnlyList<CsvImportProfile>> GetUserProfilesAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<IReadOnlyList<CsvImportProfile>>(_items.ToList());
    public Task UpsertAsync(CsvImportProfile profile, CancellationToken cancellationToken = default)
    {
        var i = _items.FindIndex(p => p.Id == profile.Id);
        if (i >= 0) _items[i] = profile; else _items.Add(profile);
        return Task.CompletedTask;
    }
    public Task ReplaceAllAsync(IEnumerable<CsvImportProfile> profiles, CancellationToken cancellationToken = default)
    {
        _items.Clear();
        _items.AddRange(profiles);
        return Task.CompletedTask;
    }
}

private static PrepareCsvImportUseCase BuildPrepare(ICsvImportProfileStore store, ITransactionRepository repo)
{
    var logger = Substitute.For<ILogger<CsvImportOrchestrator>>();
    var orchestrator = new CsvImportOrchestrator(repo, logger);
    return new PrepareCsvImportUseCase(store, orchestrator);
}
```

Facts:

- `Prepare_DkbSample_ReturnsPreview` — read `test_dkb_sample.csv`, empty store, `NeedsMapping == false`, `Preview!.Rows.Count == 4`
- `Prepare_UnknownCommaCsv_NeedsMapping` — bytes `Date,Amount,Text\n2026-03-01,1.00,A\n`, `NeedsMapping`, `Preview == null`, headers contain `Date`
- `Prepare_UserProfile_SkipsMapping` — upsert a profile for those three headers (DateFormat `yyyy-MM-dd`, Point decimal, Title=`Text`), then prepare same bytes → preview, not NeedsMapping
- `Prepare_Empty_Error` — `Array.Empty<byte>()` stream → `ErrorMessage == ImportMessageKeys.CsvNotTabular`
- `Prepare_Cancelled_Throws` — cancelled token → `OperationCanceledException`

Rewrite `CsvImportUseCaseTests` helper to `BuildOrchestrator(repo, ...)` without parsers. `Analyze_ValidRecords` calls `orchestrator.AnalyzeDtosAsync([dto], accountId: null)`. Delete `ImportFromCsv_NoParserMatches` and `ImportFromCsv_ParserThrows`. Replace `ImportFromCsvAsync` tests with AnalyzeDtos + Commit.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~PrepareCsvImportUseCaseTests|FullyQualifiedName~CsvImportUseCaseTests" --nologo`

Expected: FAIL (Prepare missing; orchestrator ctor still requires parsers)

- [ ] **Step 3: Implement Prepare + orchestrator refactor**

Keep `CreateTransaction` / duplicate / categorize code untouched. Only replace `ParseDtosAsync`.

If `AnalyzeCsvAsync(Stream)` remains for any caller, implement it via Prepare: if `NeedsMapping`, return `{ ErrorMessage = ImportMessageKeys.CsvNeedsMapping }` for non-UI callers. Add key `CsvNeedsMapping = "Msg_ImportCsvNeedsMapping"` (UI must not show this; coordinator uses `NeedsMapping`).

- [ ] **Step 4: Run tests to verify they pass**

Run: same filter. Expected: PASS. Also run existing `CsvImportUseCaseTests` duplicates/categorization.

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Application/UseCases/Import \
  Finanzuebersicht.Application/DependencyInjection/ApplicationServiceCollectionExtensions.cs \
  Finanzuebersicht.Tests/Application/UseCases/Import \
  Finanzuebersicht.Core/Services/ImportMessageKeys.cs
git commit -m "$(cat <<'EOF'
feat(import): prepare CSV via profiles instead of parser zoo

EOF
)"
```

---

### Task 7: Backup and restore profiles

**Files:**
- Modify: `Finanzuebersicht.Core/Constants/BackupEntityKeys.cs` — `CsvImportProfiles = "csvImportProfiles"`
- Modify: `Finanzuebersicht.Infrastructure/Services/BackupService.cs` — optional `ICsvImportProfileStore?` like templates; write/read `DataFileNames.CsvImportProfiles`; missing zip entry → `ReplaceAllAsync([])` skip / empty list
- Modify: `Finanzuebersicht.Tests/Services/BackupServiceTests.cs` — assert zip entry when store provided
- Modify: `Finanzuebersicht.Tests/Services/BackupRestoreIntegrationTests.cs` — round-trip one user profile
- Wire store into `BackupService` DI (host already constructs BackupService via Infrastructure — check `AddSingleton<IBackupService, BackupService>()`; add ctor param, DI will inject `ICsvImportProfileStore`)

**Interfaces:**
- Consumes: `ICsvImportProfileStore.GetUserProfilesAsync` / `ReplaceAllAsync`
- Produces: profiles in ZIP; old backups without the file still restore

- [ ] **Step 1: Write failing backup tests**

Create backup with a store that has one profile; unzip; deserialize list; assert `Id`/`Columns.Date`. Restore into empty store; assert profile returned. Create backup with null store; zip may omit entry — restore must not throw.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~BackupServiceTests|FullyQualifiedName~BackupRestoreIntegrationTests" --nologo`

Expected: FAIL (no zip entry / ctor)

- [ ] **Step 3: Implement backup wiring**

Follow `ITransactionTemplateRepository` optional pattern. `DeserializeFile<List<CsvImportProfile>>(..., DataFileNames.CsvImportProfiles) ?? []`. Include count in `EntityCounts`. Snapshot/rollback: `ReplaceAllAsync` previous list on failure.

- [ ] **Step 4: Run tests to verify they pass**

Run: same filter. Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Core/Constants/BackupEntityKeys.cs \
  Finanzuebersicht.Infrastructure/Services/BackupService.cs \
  Finanzuebersicht.Tests/Services/BackupServiceTests.cs \
  Finanzuebersicht.Tests/Services/BackupRestoreIntegrationTests.cs
git commit -m "$(cat <<'EOF'
feat(backup): include CSV import profiles in ZIP backup

EOF
)"
```

---

### Task 8: Port DKB golden tests and delete DkbCsvParser

**Files:**
- Modify: `Finanzuebersicht.Tests/Services/DkbCsvParserTests.cs` → rename to `DkbCsvImportProfileTests.cs` (or replace body) using Reader + Applier + `DkbCsvImportProfile.Instance` with the same asserts as today (`test_dkb_sample`, `test_dkb_multiline`, `test_dkb_malformed` expects 2 DTOs, amounts -120 and 300)
- Delete: `Finanzuebersicht.Core/Services/DkbCsvParser.cs`
- Delete: `Finanzuebersicht.Core/Services/IStatementParser.cs` if no remaining references
- Modify: `Finanzuebersicht.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs` — remove `AddSingleton<IStatementParser, DkbCsvParser>()`
- Modify: `Finanzuebersicht.Tests/ViewModels/TransactionsViewModelTests.cs` `ImportCsv_NavigatesToPreviewRoute` in Task 9 if it still does not compile after Task 6 — if Task 6 already broke it, fix it here or in Task 6. **Fix any remaining `IStatementParser` references in this task.**

- [ ] **Step 1: Rewrite DKB tests onto the generic pipeline (they should already pass against applier)**

Keep the three facts; implementation:

```csharp
Assert.True(CsvTableReader.TryRead(File.ReadAllBytes(path), out var table, out _));
var txs = CsvMappingApplier.Apply(table!, DkbCsvImportProfile.Instance).ToList();
```

Malformed fixture: applier skips unparsable date (`MALFORMED LINE...`) → 2 rows.

- [ ] **Step 2: Run DKB tests**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~DkbCsv" --nologo`

Expected: PASS

- [ ] **Step 3: Delete parser types and DI; fix compile**

Grep `IStatementParser` and `DkbCsvParser`. Remove. Build tests project.

- [ ] **Step 4: Run full test project**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --nologo`

Expected: PASS (fix any leftover orchestrator/VM tests)

- [ ] **Step 5: Commit**

```bash
git add -u Finanzuebersicht.Core/Services \
  Finanzuebersicht.Infrastructure/DependencyInjection/InfrastructureServiceCollectionExtensions.cs \
  Finanzuebersicht.Tests
git commit -m "$(cat <<'EOF'
refactor(import): retire DkbCsvParser in favor of the DKB profile

EOF
)"
```

---

### Task 9: Session, mapping UI, coordinator, remap

**Files:**
- Modify: `Finanzuebersicht.Presentation/Services/IImportSessionStore.cs` and `ImportSessionStore.cs`
- Modify: `Finanzuebersicht.Presentation/ViewModels/TransactionImportCoordinator.cs`
- Create: `Finanzuebersicht.Presentation/ViewModels/ImportMappingViewModel.cs`
- Create: `Finanzuebersicht/Views/ImportMappingPage.xaml` + `.xaml.cs`
- Modify: `Finanzuebersicht.Presentation/Navigation/Routes.cs` — `ImportMapping = "ImportMappingPage"`
- Modify: `Finanzuebersicht/AppShell.xaml.cs` — register route
- Modify: `Finanzuebersicht/MauiProgram.cs` — `AddTransient<ImportMappingPage>()`
- Modify: `Finanzuebersicht.Presentation/DependencyInjection/PresentationServiceCollectionExtensions.cs` — `AddTransient<ImportMappingViewModel>()`
- Modify: `Finanzuebersicht.Presentation/ViewModels/ImportPreviewViewModel.cs` + `ImportPreviewPage.xaml` — remap action
- Modify: `Finanzuebersicht.Tests/ViewModels/TransactionsViewModelTests.cs` — DKB path still goes to preview; add unknown CSV → mapping route
- Create: `Finanzuebersicht.Tests/ViewModels/ImportMappingViewModelTests.cs`
- Modify: `Finanzuebersicht.Tests/ViewModels/ImportPreviewViewModelTests.cs` — remap visible iff table in session
- Modify: `ResourceKeys.cs`, `AppResources.resx`, `AppResources.de.resx`

**Interfaces:**
- Consumes: `PrepareCsvImportUseCase`, `AnalyzeCsvImportUseCase.ExecuteAsync(CsvTable, CsvImportProfile, ...)`, `ICsvImportProfileStore.UpsertAsync`
- Produces: coordinator branches; mapping continue saves profile then preview

**Session:**

```csharp
public interface IImportSessionStore
{
    void SetTable(CsvTable table, string? accountId);
    void SetActiveSession(ImportPreviewResult preview, CsvImportProfile? profile = null);
    ImportPreviewResult? GetActiveSession();
    CsvTable? GetTable();
    CsvImportProfile? GetProfile();
    string? GetAccountId();
    bool CanRemap { get; }
    void Clear();
}
```

`CanRemap` true when table is non-null.

**Coordinator `ImportCsvAsync`:**

1. Pro-Gate unchanged.
2. Pick file; open stream.
3. `var prepared = await _prepareCsvImportUseCase.ExecuteAsync(stream, selectedAccountId)`.
4. If `ErrorMessage` → alert localized key (fallback raw).
5. `_importSessionStore.SetTable(prepared.Table!, selectedAccountId)` always when table present.
6. If `NeedsMapping` → `GoToAsync(Routes.ImportMapping)`.
7. Else set preview+profile → `GoToAsync(Routes.ImportPreview)`.

**Mapping VM:**

Properties: `PreviewRows` (first 5 of `AllRows` as joined text), `HeaderRowOptions` (index + first cells), `SelectedHeaderRow`, column options (`CsvHeaderOption` with `string? Header` null = unused), `SelectedDate/Amount/Title/Purpose/AmountSign/Iban`, `DateFormatOptions` (the five spec formats), `DecimalStyleOptions`, `CanContinue` (`profile.IsComplete`), `ContinueCommand`, `CancelCommand`.

On header-row change: `table = table.WithHeaderRow(index)`; re-guess columns and formats.

Continue: build `CsvImportProfile` (new Guid unless remapping an existing **user** profile id; if current profile `IsBuiltIn`, new id and `IsBuiltIn=false`). `UpsertAsync`. `Analyze` table+profile. `SetActiveSession(preview, profile)`. If `_openedFromPreview` → `GoBackAsync`; else `GoToAsync(ImportPreview)`. Save failure → alert `Msg_ImportProfilSpeichernFehlgeschlagen`.

`_openedFromPreview`: true when session already has a preview when mapping appears.

Cancel: `GoBackAsync` (do not Clear if opened from preview).

**Preview:** `CanRemap` bound from session. Command `RemapColumns` → `GoToAsync(Routes.ImportMapping)`. Reload preview when `OnAppearing` / `LoadPreview` if `GetActiveSession()?.SessionId != _loadedSessionId` so remap refresh works. Show remap button in the summary card.

**Strings (DE / EN):**

| Key | DE | EN |
|-----|----|----|
| `Ttl_ImportZuordnung` | Spalten zuordnen | Map columns |
| `Lbl_ImportKopfzeile` | Kopfzeile | Header row |
| `Lbl_ImportSpalteDatum` | Datum | Date |
| `Lbl_ImportSpalteBetrag` | Betrag | Amount |
| `Lbl_ImportSpalteTitel` | Titel / Gegenpartei | Title / payee |
| `Lbl_ImportSpalteZweck` | Verwendungszweck | Payment reference |
| `Lbl_ImportSpalteTyp` | Typ / Vorzeichen | Type / sign |
| `Lbl_ImportSpalteIban` | IBAN | IBAN |
| `Lbl_ImportDatumsformat` | Datumsformat | Date format |
| `Lbl_ImportDezimalstil` | Dezimalzahlen | Decimal numbers |
| `Lbl_ImportDezimalKomma` | 1.234,56 | 1.234,56 |
| `Lbl_ImportDezimalPunkt` | 1,234.56 | 1,234.56 |
| `Lbl_ImportSpalteKeine` | — nicht verwendet — | — not used — |
| `Btn_ImportWeiter` | Weiter | Continue |
| `Btn_ImportSpaltenNeu` | Spalten neu zuordnen | Remap columns |
| `Msg_ImportCsvNotTabular` | Die Datei ist leer oder kein gültiges CSV. | The file is empty or not a valid CSV. |
| `Msg_ImportCsvNeedsMapping` | Die Spalten dieser Datei müssen zugeordnet werden. | This file’s columns need to be mapped. |
| `Msg_ImportProfilSpeichernFehlgeschlagen` | Das Import-Profil konnte nicht gespeichert werden. | The import profile could not be saved. |

Change `Msg_ImportNoParserMatched` DE/EN to unused or leave for safety; coordinator must not use it for unknown CSV.

Mapping page: `SelectionField` for every dropdown. Continue button `IsEnabled="{Binding CanContinue}"`. No Toolkit popup.

- [ ] **Step 1: Write failing VM tests**

Coordinator/Transactions: unknown comma CSV navigates to `Routes.ImportMapping`; DKB fixture bytes navigate to `Routes.ImportPreview`. Mapping VM: Continue disabled until date+amount+title; after continue, store `UpsertAsync` received and navigation to preview. Preview: `CanRemap` true when `SetTable` was called.

Use a fake `ICsvImportProfileStore` and real `CsvTableReader` / Prepare with that store.

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --filter "FullyQualifiedName~ImportMappingViewModelTests|FullyQualifiedName~ImportPreviewViewModelTests|FullyQualifiedName~ImportCsv_Navigates" --nologo`

Expected: FAIL

- [ ] **Step 3: Implement session, pages, coordinator, strings, DI, routes**

Wire `PrepareCsvImportUseCase` into `TransactionImportCoordinator` (constructor + `TransactionsViewModel` test `CreateSut` if it constructs the coordinator manually — match existing factory).

- [ ] **Step 4: Run tests to verify they pass**

Run: `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --nologo`

Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add Finanzuebersicht.Presentation Finanzuebersicht/Views \
  Finanzuebersicht/AppShell.xaml.cs Finanzuebersicht/MauiProgram.cs \
  Finanzuebersicht/Resources/Strings Finanzuebersicht.Tests/ViewModels
git commit -m "$(cat <<'EOF'
feat(import): mapping UI and remap for unknown CSV fingerprints

EOF
)"
```

---

### Task 10: Docs

**Files:**
- Modify: `.github/copilot-instructions.md` Import bullet — generic mapping + DKB profile, not `DkbCsvParser`
- Modify: `docs/GUIDE.md` CSV sentence — mapping for unknown banks; DKB automatic
- Modify: `.cursor/rules/app-domain.mdc` only if it mentions DKB-only import (skip if it does not)

- [ ] **Step 1: Update the Import section in copilot-instructions.md**

Replace the parser bullet with: CSV import via table reader + column-mapping profiles (built-in DKB, user JSON), preview, auto-categorization. Mapping UI for unknown fingerprints. No Open Banking.

- [ ] **Step 2: Update GUIDE.md import sentence**

`CSV-Import durchführen (Spalten-Mapping, Auto-Kategorisierung; DKB ohne Mapping)`

- [ ] **Step 3: Commit**

```bash
git add .github/copilot-instructions.md docs/GUIDE.md
git commit -m "$(cat <<'EOF'
docs: describe generic CSV column-mapping import

EOF
)"
```

Do not close #360 in git; leave that to the PR.

---

## Self-review (spec coverage)

| Spec item | Task |
|-----------|------|
| Reader encoding/delimiter/header | 1 |
| Fingerprint silent match | 2, 5, 6 |
| DKB built-in exact headers | 2, 4, 8 |
| Optional type column | 4 |
| Fields date/amount/title\|purpose/IBAN | 2, 4, 9 |
| Guesser + format detect | 3, 9 |
| NeedsMapping UI | 6, 9 |
| Remap overwrites user / shadows DKB | 5, 9 |
| Backup JSON | 7 |
| Pro-Gate / SelectionField / Resx | 9, Global |
| Delete DkbCsvParser | 8 |
| Analyze/Commit unchanged | 6 |
| CAMT / Open Banking / Settings list out | — omitted |
| Tests listed in spec | 1–9 |

No `IStatementParser` after Task 8. `Msg_ImportNoParserMatched` is not the unknown-file happy path.
