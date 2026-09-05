//
//  CloudKitSyncBridge.swift
//  Finanzübersicht — CloudKit Sync MVP (#243)
//
//  C ABI bridge around CKSyncEngine for the MAUI host (see CloudKitSyncTransport.cs).
//  CKSyncEngine is Swift-only, so every call goes through @_cdecl entry points.
//
//  Container: iCloud.de.thomasmenzl.finanzuebersicht  (private database)
//  Zone:      finanzuebersicht-sync                   (custom zone, current user)
//
//  Record types: Account, Category, Transaction, RecurringTransaction, SparZiel
//    - payload   (String) — camelCase entity JSON
//    - updatedAt (Date)   — last local write, drives last-write-wins
//    - recordName == entity Id
//
//  Record type Tombstone — durable deletion marker
//    - entityType (String) — one of the entity record types
//    - deletedAt  (Date)
//    - recordName == "tombstone-" + entity Id (a CloudKit recordName is unique per zone
//      across record types, so the marker cannot reuse the deleted entity's name)
//
//  The bundle needs the iCloud container entitlement before any of this can succeed —
//  that is wired up separately (Task 9). Without it CloudKit calls fail with
//  CKError.missingEntitlement and the bridge reports a non-zero error code.
//
//  Build: build-cloudkit-sync-bridge.sh (device / simulator / Mac Catalyst, min iOS 15.0).
//  CKSyncEngine itself needs iOS 17 / macOS 14, hence the @available walls below.
//

import CloudKit
import Foundation

// MARK: - Configuration

private enum CKBridgeConfig {
    static let containerIdentifier = "iCloud.de.thomasmenzl.finanzuebersicht"
    static let zoneName = "finanzuebersicht-sync"

    static let tombstoneRecordType = "Tombstone"
    static let tombstoneRecordNamePrefix = "tombstone-"

    static let payloadField = "payload"
    static let updatedAtField = "updatedAt"
    static let entityTypeField = "entityType"
    static let deletedAtField = "deletedAt"

    static let stateDefaultsKey = "de.thomasmenzl.finanzuebersicht.cloudkit.syncEngineState"

    /// Index == SyncEntityType ordinal in C# (Account = 0 … SparZiel = 4).
    static let entityRecordTypes = ["Account", "Category", "Transaction", "RecurringTransaction", "SparZiel"]

    static func entityTypeOrdinal(forRecordType recordType: String) -> Int? {
        entityRecordTypes.firstIndex(of: recordType)
    }

    static func isKnownEntityRecordType(_ recordType: String) -> Bool {
        entityRecordTypes.contains(recordType)
    }
}

/// Result codes shared with the managed side. 0 == ok.
private enum CKBridgeStatus {
    static let ok: Int32 = 0
    static let unsupportedOS: Int32 = 1
    static let notStarted: Int32 = 2
    static let invalidArgument: Int32 = 3
    static let cloudKitFailure: Int32 = 4
    static let unknownFailure: Int32 = 5
}

private struct CKBridgeError: Error {
    let status: Int32
}

private func ckBridgeStatus(for error: Error) -> Int32 {
    if let bridgeError = error as? CKBridgeError {
        return bridgeError.status
    }
    if error is CKError {
        return CKBridgeStatus.cloudKitFailure
    }
    return CKBridgeStatus.unknownFailure
}

// MARK: - ISO-8601 helpers

private enum CKBridgeDates {
    private static let withFraction: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        return formatter
    }()

    private static let withoutFraction: ISO8601DateFormatter = {
        let formatter = ISO8601DateFormatter()
        formatter.formatOptions = [.withInternetDateTime]
        formatter.timeZone = TimeZone(secondsFromGMT: 0)
        return formatter
    }()

    static func string(from date: Date) -> String {
        withFraction.string(from: date)
    }

    static func date(from text: String) -> Date? {
        withFraction.date(from: text) ?? withoutFraction.date(from: text)
    }
}

// MARK: - Managed callback

private typealias CKBridgeRecordsCallback = @convention(c) (UnsafePointer<CChar>?) -> Void

/// The managed side keeps its delegate rooted; we only hold the raw function pointer.
private final class CKBridgeCallbackStore {
    static let shared = CKBridgeCallbackStore()

    private let lock = NSLock()
    private var callback: CKBridgeRecordsCallback?

    func set(_ callback: CKBridgeRecordsCallback?) {
        lock.lock()
        self.callback = callback
        lock.unlock()
    }

    /// The C string is only valid for the duration of the call — the managed side copies it.
    func post(json: String) {
        lock.lock()
        let callback = self.callback
        lock.unlock()

        guard let callback else { return }
        json.withCString { callback($0) }
    }
}

// MARK: - Blocking bridge for the synchronous C API

private final class CKBridgeResultBox {
    var value: Int32 = CKBridgeStatus.unknownFailure
}

/// Synchronous work that only touches CKSyncEngine.State — no thread hop needed.
private func ckBridgeRun(_ work: () throws -> Void) -> Int32 {
    do {
        try work()
        return CKBridgeStatus.ok
    } catch {
        return ckBridgeStatus(for: error)
    }
}

/// The C API is synchronous; CloudKit is not. Callers on the managed side dispatch to a
/// background thread (Task.Run), so blocking here does not stall the UI.
private func ckBridgeRunBlocking(_ work: @escaping () async throws -> Int32) -> Int32 {
    let box = CKBridgeResultBox()
    let semaphore = DispatchSemaphore(value: 0)

    Task.detached(priority: .userInitiated) {
        do {
            box.value = try await work()
        } catch {
            box.value = ckBridgeStatus(for: error)
        }
        semaphore.signal()
    }

    semaphore.wait()
    return box.value
}

// MARK: - Container (usable below iOS 17 — plain CloudKit, no CKSyncEngine)

private enum CKBridgeContainer {
    static let shared = CKContainer(identifier: CKBridgeConfig.containerIdentifier)
}

// MARK: - Sync engine host

@available(iOS 17.0, macOS 14.0, *)
private final class CKSyncEngineHost: NSObject, CKSyncEngineDelegate {
    static let shared = CKSyncEngineHost()

    private let container = CKBridgeContainer.shared
    private let lock = NSLock()

    // All three are guarded by `lock`; CKSyncEngineDelegate is Sendable, so opt out explicitly.
    private nonisolated(unsafe) var engine: CKSyncEngine?
    /// Last known server copy per record, so staged saves keep their change tag and
    /// CloudKit does not reject them as conflicting.
    private nonisolated(unsafe) var knownRecords: [CKRecord.ID: CKRecord] = [:]
    /// Records staged by enqueue_* and handed to CKSyncEngine in nextRecordZoneChangeBatch.
    private nonisolated(unsafe) var stagedRecords: [CKRecord.ID: CKRecord] = [:]

    private var database: CKDatabase { container.privateCloudDatabase }

    private var zoneID: CKRecordZone.ID {
        CKRecordZone.ID(zoneName: CKBridgeConfig.zoneName, ownerName: CKCurrentUserDefaultName)
    }

    // MARK: Lifecycle

    func start() throws {
        lock.lock()
        let alreadyRunning = engine != nil
        lock.unlock()
        if alreadyRunning { return }

        var configuration = CKSyncEngine.Configuration(
            database: database,
            stateSerialization: Self.loadState(),
            delegate: self
        )
        // The C# CloudSyncOrchestrator decides when to fetch/send.
        configuration.automaticallySync = false

        let created = CKSyncEngine(configuration)

        lock.lock()
        engine = created
        lock.unlock()

        created.state.add(pendingDatabaseChanges: [.saveZone(CKRecordZone(zoneID: zoneID))])
    }

    func stop() {
        lock.lock()
        engine = nil
        stagedRecords.removeAll()
        lock.unlock()
    }

    private func requireEngine() throws -> CKSyncEngine {
        lock.lock()
        let engine = self.engine
        lock.unlock()

        guard let engine else {
            throw CKBridgeError(status: CKBridgeStatus.notStarted)
        }
        return engine
    }

    // MARK: State persistence

    private static func loadState() -> CKSyncEngine.State.Serialization? {
        guard let data = UserDefaults.standard.data(forKey: CKBridgeConfig.stateDefaultsKey) else {
            return nil
        }
        return try? JSONDecoder().decode(CKSyncEngine.State.Serialization.self, from: data)
    }

    private static func store(state: CKSyncEngine.State.Serialization) {
        guard let data = try? JSONEncoder().encode(state) else { return }
        UserDefaults.standard.set(data, forKey: CKBridgeConfig.stateDefaultsKey)
    }

    // MARK: Queries

    func isZoneEmpty() async throws -> Bool {
        do {
            let changes = try await database.recordZoneChanges(inZoneWith: zoneID, since: nil, resultsLimit: 1)
            return changes.modificationResultsByID.isEmpty
        } catch let error as CKError {
            switch error.code {
            case .zoneNotFound, .userDeletedZone, .unknownItem:
                return true
            default:
                throw error
            }
        }
    }

    // MARK: Staging

    func enqueueUpsert(recordType: String, id: String, updatedAt: Date, payloadJson: String) throws {
        let engine = try requireEngine()
        let recordID = CKRecord.ID(recordName: id, zoneID: zoneID)
        let record = baseRecord(for: recordID, recordType: recordType)
        record[CKBridgeConfig.payloadField] = payloadJson as CKRecordValue
        record[CKBridgeConfig.updatedAtField] = updatedAt as CKRecordValue

        stage(record)
        engine.state.add(pendingRecordZoneChanges: [.saveRecord(recordID)])
    }

    func enqueueDelete(recordType: String, id: String, deletedAt: Date) throws {
        let engine = try requireEngine()

        let tombstoneID = CKRecord.ID(
            recordName: CKBridgeConfig.tombstoneRecordNamePrefix + id,
            zoneID: zoneID
        )
        let tombstone = baseRecord(for: tombstoneID, recordType: CKBridgeConfig.tombstoneRecordType)
        tombstone[CKBridgeConfig.entityTypeField] = recordType as CKRecordValue
        tombstone[CKBridgeConfig.deletedAtField] = deletedAt as CKRecordValue
        stage(tombstone)

        let entityID = CKRecord.ID(recordName: id, zoneID: zoneID)
        unstage(entityID)

        engine.state.add(pendingRecordZoneChanges: [
            .saveRecord(tombstoneID),
            .deleteRecord(entityID)
        ])
    }

    /// Reuses the last known server record (change tag intact) or creates a fresh one.
    private func baseRecord(for recordID: CKRecord.ID, recordType: String) -> CKRecord {
        lock.lock()
        let known = stagedRecords[recordID] ?? knownRecords[recordID]
        lock.unlock()

        if let known, known.recordType == recordType {
            return known
        }
        return CKRecord(recordType: recordType, recordID: recordID)
    }

    private func stage(_ record: CKRecord) {
        lock.lock()
        stagedRecords[record.recordID] = record
        lock.unlock()
    }

    private func unstage(_ recordID: CKRecord.ID) {
        lock.lock()
        stagedRecords.removeValue(forKey: recordID)
        knownRecords.removeValue(forKey: recordID)
        lock.unlock()
    }

    private func stagedRecord(for recordID: CKRecord.ID) -> CKRecord? {
        lock.lock()
        let record = stagedRecords[recordID]
        lock.unlock()
        return record
    }

    private func remember(_ record: CKRecord) {
        lock.lock()
        knownRecords[record.recordID] = record
        stagedRecords.removeValue(forKey: record.recordID)
        lock.unlock()
    }

    // MARK: Sync

    func fetchChanges() async throws {
        try await requireEngine().fetchChanges()
    }

    func sendChanges() async throws {
        try await requireEngine().sendChanges()
    }

    // MARK: CKSyncEngineDelegate

    func handleEvent(_ event: CKSyncEngine.Event, syncEngine: CKSyncEngine) async {
        switch event {
        case .stateUpdate(let update):
            Self.store(state: update.stateSerialization)

        case .fetchedRecordZoneChanges(let changes):
            handleFetched(changes)

        case .sentRecordZoneChanges(let sent):
            handleSent(sent, syncEngine: syncEngine)

        default:
            // accountChange / fetchedDatabaseChanges / willFetch* / didSend* … nothing to do
            // for the MVP: the managed orchestrator drives everything explicitly.
            break
        }
    }

    func nextRecordZoneChangeBatch(
        _ context: CKSyncEngine.SendChangesContext,
        syncEngine: CKSyncEngine
    ) async -> CKSyncEngine.RecordZoneChangeBatch? {
        let scope = context.options.scope
        let pending = syncEngine.state.pendingRecordZoneChanges.filter { scope.contains($0) }

        return await CKSyncEngine.RecordZoneChangeBatch(pendingChanges: pending) { recordID in
            guard let record = self.stagedRecord(for: recordID) else {
                // Nothing staged (e.g. after stop) — drop the pending change instead of
                // letting CKSyncEngine retry it forever.
                syncEngine.state.remove(pendingRecordZoneChanges: [.saveRecord(recordID)])
                return nil
            }
            return record
        }
    }

    // MARK: Event handling

    private func handleFetched(_ changes: CKSyncEngine.Event.FetchedRecordZoneChanges) {
        var batch: [[String: Any]] = []

        for modification in changes.modifications {
            let record = modification.record
            remember(record)
            if let dto = Self.dto(from: record) {
                batch.append(dto)
            }
        }

        for deletion in changes.deletions {
            unstage(deletion.recordID)
            if let dto = Self.dto(fromDeletionOf: deletion.recordID, recordType: deletion.recordType) {
                batch.append(dto)
            }
        }

        post(batch)
    }

    private func handleSent(_ sent: CKSyncEngine.Event.SentRecordZoneChanges, syncEngine: CKSyncEngine) {
        for save in sent.savedRecords {
            remember(save)
        }

        for deletion in sent.deletedRecordIDs {
            unstage(deletion)
        }

        for failure in sent.failedRecordSaves {
            let recordID = failure.record.recordID
            switch failure.error.code {
            case .serverRecordChanged:
                // Re-stage our payload on top of the server copy and retry next send.
                if let serverRecord = failure.error.serverRecord {
                    serverRecord[CKBridgeConfig.payloadField] = failure.record[CKBridgeConfig.payloadField]
                    serverRecord[CKBridgeConfig.updatedAtField] = failure.record[CKBridgeConfig.updatedAtField]
                    serverRecord[CKBridgeConfig.entityTypeField] = failure.record[CKBridgeConfig.entityTypeField]
                    serverRecord[CKBridgeConfig.deletedAtField] = failure.record[CKBridgeConfig.deletedAtField]
                    stage(serverRecord)
                    syncEngine.state.add(pendingRecordZoneChanges: [.saveRecord(recordID)])
                }
            case .zoneNotFound:
                // Zone vanished (account reset) — recreate it and retry.
                syncEngine.state.add(pendingDatabaseChanges: [.saveZone(CKRecordZone(zoneID: zoneID))])
                syncEngine.state.add(pendingRecordZoneChanges: [.saveRecord(recordID)])
            case .unknownItem:
                unstage(recordID)
            default:
                // Retryable / fatal errors are surfaced by CKSyncEngine's own retry logic.
                break
            }
        }
    }

    private func post(_ batch: [[String: Any]]) {
        guard !batch.isEmpty else { return }
        guard let data = try? JSONSerialization.data(withJSONObject: batch, options: []),
              let json = String(data: data, encoding: .utf8)
        else {
            return
        }
        CKBridgeCallbackStore.shared.post(json: json)
    }

    // MARK: DTO mapping (camelCase, matches CloudSyncRecordDto)

    private static func dto(from record: CKRecord) -> [String: Any]? {
        if record.recordType == CKBridgeConfig.tombstoneRecordType {
            guard let entityType = record[CKBridgeConfig.entityTypeField] as? String,
                  let ordinal = CKBridgeConfig.entityTypeOrdinal(forRecordType: entityType)
            else {
                return nil
            }

            let id = String(record.recordID.recordName.dropFirst(
                CKBridgeConfig.tombstoneRecordNamePrefix.count))
            guard !id.isEmpty else { return nil }

            let deletedAt = record[CKBridgeConfig.deletedAtField] as? Date ?? Date()
            return [
                "entityType": ordinal,
                "id": id,
                "isTombstone": true,
                "updatedAt": CKBridgeDates.string(from: deletedAt),
                "deletedAt": CKBridgeDates.string(from: deletedAt)
            ]
        }

        guard let ordinal = CKBridgeConfig.entityTypeOrdinal(forRecordType: record.recordType),
              let payload = record[CKBridgeConfig.payloadField] as? String
        else {
            return nil
        }

        var dto: [String: Any] = [
            "entityType": ordinal,
            "id": record.recordID.recordName,
            "isTombstone": false,
            "payloadJson": payload
        ]
        if let updatedAt = record[CKBridgeConfig.updatedAtField] as? Date {
            dto["updatedAt"] = CKBridgeDates.string(from: updatedAt)
        }
        return dto
    }

    /// A hard delete of an entity record (no Tombstone written, e.g. by an older client).
    private static func dto(fromDeletionOf recordID: CKRecord.ID, recordType: String) -> [String: Any]? {
        guard let ordinal = CKBridgeConfig.entityTypeOrdinal(forRecordType: recordType) else {
            return nil
        }

        let now = CKBridgeDates.string(from: Date())
        return [
            "entityType": ordinal,
            "id": recordID.recordName,
            "isTombstone": true,
            "updatedAt": now,
            "deletedAt": now
        ]
    }
}

// MARK: - Account status (works below iOS 17 — plain CKContainer)

private func ckBridgeAccountStatusOrdinal(_ status: CKAccountStatus) -> Int32 {
    // Must match CloudSyncAccountStatus in C#.
    switch status {
    case .available: return 0
    case .noAccount: return 1
    case .restricted: return 2
    case .couldNotDetermine: return 3
    case .temporarilyUnavailable: return 4
    @unknown default: return 3
    }
}

// MARK: - C ABI

@_cdecl("finanzuebersicht_ck_is_supported")
public func finanzuebersicht_ck_is_supported() -> Int32 {
    if #available(iOS 17.0, macOS 14.0, *) {
        return 1
    }
    return 0
}

/// Returns the CloudSyncAccountStatus ordinal (never an error code); 3 == CouldNotDetermine.
@_cdecl("finanzuebersicht_ck_account_status")
public func finanzuebersicht_ck_account_status() -> Int32 {
    let box = CKBridgeResultBox()
    box.value = 3
    let semaphore = DispatchSemaphore(value: 0)

    Task.detached(priority: .userInitiated) {
        do {
            box.value = ckBridgeAccountStatusOrdinal(try await CKBridgeContainer.shared.accountStatus())
        } catch {
            box.value = 3
        }
        semaphore.signal()
    }

    semaphore.wait()
    return box.value
}

/// 1 == empty, 0 == has records, negative == error.
@_cdecl("finanzuebersicht_ck_is_zone_empty")
public func finanzuebersicht_ck_is_zone_empty() -> Int32 {
    if #available(iOS 17.0, macOS 14.0, *) {
        let result = ckBridgeRunBlocking {
            try await CKSyncEngineHost.shared.isZoneEmpty() ? 1 : 0
        }
        // ckBridgeRunBlocking maps thrown errors onto positive status codes; flip them.
        return result > 1 ? -result : result
    }
    return -CKBridgeStatus.unsupportedOS
}

@_cdecl("finanzuebersicht_ck_start")
public func finanzuebersicht_ck_start() -> Int32 {
    if #available(iOS 17.0, macOS 14.0, *) {
        return ckBridgeRun { try CKSyncEngineHost.shared.start() }
    }
    return CKBridgeStatus.unsupportedOS
}

@_cdecl("finanzuebersicht_ck_stop")
public func finanzuebersicht_ck_stop() -> Int32 {
    if #available(iOS 17.0, macOS 14.0, *) {
        CKSyncEngineHost.shared.stop()
        return CKBridgeStatus.ok
    }
    return CKBridgeStatus.unsupportedOS
}

@_cdecl("finanzuebersicht_ck_enqueue_upsert")
public func finanzuebersicht_ck_enqueue_upsert(
    _ type: UnsafePointer<CChar>?,
    _ id: UnsafePointer<CChar>?,
    _ updatedAtIso: UnsafePointer<CChar>?,
    _ payloadJson: UnsafePointer<CChar>?
) -> Int32 {
    guard #available(iOS 17.0, macOS 14.0, *) else {
        return CKBridgeStatus.unsupportedOS
    }
    guard let type, let id, let updatedAtIso, let payloadJson else {
        return CKBridgeStatus.invalidArgument
    }

    // Copy out of caller memory immediately — the managed marshaller frees it on return.
    let recordType = String(cString: type)
    let recordName = String(cString: id)
    let payload = String(cString: payloadJson)

    guard CKBridgeConfig.isKnownEntityRecordType(recordType),
          !recordName.isEmpty,
          let updatedAt = CKBridgeDates.date(from: String(cString: updatedAtIso))
    else {
        return CKBridgeStatus.invalidArgument
    }

    return ckBridgeRun {
        try CKSyncEngineHost.shared.enqueueUpsert(
            recordType: recordType,
            id: recordName,
            updatedAt: updatedAt,
            payloadJson: payload
        )
    }
}

@_cdecl("finanzuebersicht_ck_enqueue_delete")
public func finanzuebersicht_ck_enqueue_delete(
    _ type: UnsafePointer<CChar>?,
    _ id: UnsafePointer<CChar>?,
    _ deletedAtIso: UnsafePointer<CChar>?
) -> Int32 {
    guard #available(iOS 17.0, macOS 14.0, *) else {
        return CKBridgeStatus.unsupportedOS
    }
    guard let type, let id, let deletedAtIso else {
        return CKBridgeStatus.invalidArgument
    }

    let recordType = String(cString: type)
    let recordName = String(cString: id)

    guard CKBridgeConfig.isKnownEntityRecordType(recordType),
          !recordName.isEmpty,
          let deletedAt = CKBridgeDates.date(from: String(cString: deletedAtIso))
    else {
        return CKBridgeStatus.invalidArgument
    }

    return ckBridgeRun {
        try CKSyncEngineHost.shared.enqueueDelete(
            recordType: recordType,
            id: recordName,
            deletedAt: deletedAt
        )
    }
}

@_cdecl("finanzuebersicht_ck_fetch_changes")
public func finanzuebersicht_ck_fetch_changes() -> Int32 {
    if #available(iOS 17.0, macOS 14.0, *) {
        return ckBridgeRunBlocking {
            try await CKSyncEngineHost.shared.fetchChanges()
            return CKBridgeStatus.ok
        }
    }
    return CKBridgeStatus.unsupportedOS
}

@_cdecl("finanzuebersicht_ck_send_changes")
public func finanzuebersicht_ck_send_changes() -> Int32 {
    if #available(iOS 17.0, macOS 14.0, *) {
        return ckBridgeRunBlocking {
            try await CKSyncEngineHost.shared.sendChanges()
            return CKBridgeStatus.ok
        }
    }
    return CKBridgeStatus.unsupportedOS
}

@_cdecl("finanzuebersicht_ck_set_records_callback")
public func finanzuebersicht_ck_set_records_callback(
    _ callback: (@convention(c) (UnsafePointer<CChar>?) -> Void)?
) {
    CKBridgeCallbackStore.shared.set(callback)
}
