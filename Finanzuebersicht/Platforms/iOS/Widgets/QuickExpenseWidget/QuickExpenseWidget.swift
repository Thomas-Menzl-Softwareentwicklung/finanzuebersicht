import WidgetKit
import SwiftUI
import AppIntents

/// Shared with the MAUI host — must match Finanzuebersicht.Core.Constants.AppGroupIds.
enum AppGroup {
    static let id = "group.de.thomasmenzl.finanzuebersicht"
    static let pendingFileName = "quick-expense-pending.json"
    static let hasProKey = "hasPro"
}

struct PendingExpense: Codable, Identifiable {
    var id: String
    var amountText: String
    var title: String
    var createdAt: Date
}

enum QuickExpenseSharedStore {
    private static let iso8601: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime, .withFractionalSeconds]
        return f
    }()

    private static let iso8601NoFraction: ISO8601DateFormatter = {
        let f = ISO8601DateFormatter()
        f.formatOptions = [.withInternetDateTime]
        return f
    }()

    static var containerURL: URL? {
        FileManager.default.containerURL(forSecurityApplicationGroupIdentifier: AppGroup.id)
    }

    static var pendingURL: URL? {
        containerURL?.appendingPathComponent(AppGroup.pendingFileName)
    }

    static var hasPro: Bool {
        UserDefaults(suiteName: AppGroup.id)?.bool(forKey: AppGroup.hasProKey) ?? false
    }

    static func enqueue(amountText: String, title: String) throws {
        guard let url = pendingURL else {
            throw NSError(domain: "QuickExpense", code: 1, userInfo: [
                NSLocalizedDescriptionKey: "App Group container unavailable"
            ])
        }

        var items: [PendingExpense] = []
        if FileManager.default.fileExists(atPath: url.path),
           let data = try? Data(contentsOf: url),
           let decoded = try? decodePending(data) {
            items = decoded
        }

        items.append(PendingExpense(
            id: UUID().uuidString,
            amountText: amountText,
            title: title,
            createdAt: Date()
        ))

        try encodePending(items).write(to: url, options: [.atomic])
    }

    private static func decodePending(_ data: Data) throws -> [PendingExpense] {
        let decoder = JSONDecoder()
        decoder.dateDecodingStrategy = .custom { decoder in
            let container = try decoder.singleValueContainer()
            let raw = try container.decode(String.self)
            if let d = iso8601.date(from: raw) ?? iso8601NoFraction.date(from: raw) {
                return d
            }
            throw DecodingError.dataCorruptedError(in: container, debugDescription: "Invalid date: \(raw)")
        }
        return try decoder.decode([PendingExpense].self, from: data)
    }

    private static func encodePending(_ items: [PendingExpense]) throws -> Data {
        let encoder = JSONEncoder()
        encoder.dateEncodingStrategy = .iso8601
        encoder.outputFormatting = [.sortedKeys]
        return try encoder.encode(items)
    }
}

struct SaveQuickExpenseIntent: AppIntent {
    static var title: LocalizedStringResource = "Save quick expense"
    static var description = IntentDescription("Captures amount and note for Finanzübersicht.")

    @Parameter(title: "Amount")
    var amountText: String

    @Parameter(title: "Note")
    var title: String

    init() {
        self.amountText = ""
        self.title = ""
    }

    init(amountText: String, title: String) {
        self.amountText = amountText
        self.title = title
    }

    func perform() async throws -> some IntentResult & ProvidesDialog {
        guard QuickExpenseSharedStore.hasPro else {
            return .result(dialog: "Finanzübersicht Pro is required for quick capture.")
        }

        let trimmedAmount = amountText.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedTitle = title.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmedAmount.isEmpty, !trimmedTitle.isEmpty else {
            return .result(dialog: "Amount and note are required.")
        }

        try QuickExpenseSharedStore.enqueue(amountText: trimmedAmount, title: trimmedTitle)
        return .result(dialog: "Saved — open the app to sync.")
    }
}

struct QuickExpenseProvider: TimelineProvider {
    func placeholder(in context: Context) -> QuickExpenseEntry {
        QuickExpenseEntry(date: Date(), hasPro: true)
    }

    func getSnapshot(in context: Context, completion: @escaping (QuickExpenseEntry) -> Void) {
        completion(QuickExpenseEntry(date: Date(), hasPro: QuickExpenseSharedStore.hasPro))
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<QuickExpenseEntry>) -> Void) {
        let entry = QuickExpenseEntry(date: Date(), hasPro: QuickExpenseSharedStore.hasPro)
        completion(Timeline(entries: [entry], policy: .after(Date().addingTimeInterval(3600))))
    }
}

struct QuickExpenseEntry: TimelineEntry {
    let date: Date
    let hasPro: Bool
}

struct QuickExpenseWidgetEntryView: View {
    var entry: QuickExpenseEntry

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text("Quick expense")
                .font(.headline)
            if entry.hasPro {
                Text("Tap a preset — open the app to sync")
                    .font(.caption)
                    .foregroundStyle(.secondary)
                HStack(spacing: 6) {
                    Button(intent: SaveQuickExpenseIntent(amountText: "3.50", title: "Coffee")) {
                        Text("Coffee 3.50")
                            .font(.caption2)
                    }
                    .buttonStyle(.borderedProminent)
                    Button(intent: SaveQuickExpenseIntent(amountText: "5.00", title: "Snack")) {
                        Text("Snack 5")
                            .font(.caption2)
                    }
                    .buttonStyle(.bordered)
                }
            } else {
                Text("Pro unlocks Home Screen capture")
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .containerBackground(.fill.tertiary, for: .widget)
    }
}

@main
struct QuickExpenseWidget: Widget {
    let kind = "QuickExpenseWidget"

    var body: some WidgetConfiguration {
        StaticConfiguration(kind: kind, provider: QuickExpenseProvider()) { entry in
            QuickExpenseWidgetEntryView(entry: entry)
        }
        .configurationDisplayName("Quick expense")
        .description("Capture a small expense without opening the full app.")
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}
