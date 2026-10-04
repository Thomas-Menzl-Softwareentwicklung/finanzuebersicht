# CSV Import Profile CloudKit Sync — Implementation Plan

> **For agentic workers:** Execute task-by-task. Steps use checkbox syntax.

**Goal:** Sync user CSV import profiles via CloudKit so mappings follow the user across devices.

**Tech:** Existing CKSyncEngine bridge, `SyncEntityType`, LWW orchestrator.

## Files

| File | Change |
|------|--------|
| `SyncEntityType.cs` | Add `CsvImportProfile = 6` |
| `CsvImportProfile.cs` | Add `UpdatedAt` |
| `ICsvImportProfileStore.cs` + `FileCsvImportProfileStore` | `DeleteAsync` |
| `CloudKitSyncBridgeCodec.cs` + tests | Record type map |
| `CloudKitSyncBridge.swift` | Type + ordinal 6 |
| `CloudSyncOrchestrator*.cs` | Persist / enqueue |
| `EnableCloudSyncUseCase.cs` | Empty check + seed |
| `ClearLocalSyncedDataUseCase.cs` | Clear profiles |
| `UpsertCsvImportProfileUseCase.cs` | New |
| `ImportMappingViewModel.cs` | Call use case |
| DI registration | Use case |
| Docs README-CloudKitSync / design import spec line | Mention type |

## Tasks

### Task 1: Core model + codec
- [ ] `UpdatedAt` on profile; enum value 6; codec map; failing codec test then pass

### Task 2: Store DeleteAsync
- [ ] Interface + file store + in-memory test doubles

### Task 3: Swift bridge
- [ ] `CsvImportProfile` in known types; ordinal 6 via switch (not `firstIndex`)

### Task 4: Orchestrator + enable/clear
- [ ] Inject `ICsvImportProfileStore`; wire all switch arms; seed/clear/empty

### Task 5: Upsert use case + mapping VM
- [ ] Stamp/notify; VM uses use case

### Task 6: Tests + docs
- [ ] Codec, enable empty/seed, store delete; update CloudKit README entity list
