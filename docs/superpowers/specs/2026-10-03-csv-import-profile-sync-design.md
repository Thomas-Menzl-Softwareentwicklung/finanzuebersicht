# CSV Import Profile CloudKit Sync — Design

**Date:** 2026-10-03  
**Status:** Approved  
**Related:** Generic CSV import (`docs/superpowers/specs/2026-09-11-generic-csv-import-design.md`), CloudKit MVP #243

## Problem

User CSV column mappings live only in `csv-import-profiles.json`. With iCloud Sync, each device must remap the same bank CSV again.

## Decision

Sync each **user** profile as its own CloudKit record (same LWW pattern as SparZiel). Built-in DKB never syncs.

## CloudKit

| Item | Value |
|------|--------|
| Record type | `CsvImportProfile` |
| Fields | `payload` (String), `updatedAt` (Date) — same as other entity types |
| `recordName` | profile `Id` |
| Zone | `finanzuebersicht-sync` (unchanged) |
| Portal | Developer creates type in Development, deploys to Production |
| `schemaVersion` | **not** bumped — older apps ignore unknown record types |

## App model

- `SyncEntityType.CsvImportProfile = 6` (`SyncMeta` stays `5`)
- Swift bridge: explicit ordinal map (not array index — index 5 would collide with SyncMeta)
- `CsvImportProfile.UpdatedAt` for LWW
- `UpsertCsvImportProfileUseCase`: stamp + persist + `NotifyLocalUpsert`
- Orchestrator / `EnableCloudSyncUseCase` / `ClearLocalSyncedDataUseCase` treat profiles like SparZiele
- Store: add `DeleteAsync` for remote tombstones

## Out of scope

- Profile list/delete UI in Settings
- Syncing built-in DKB
