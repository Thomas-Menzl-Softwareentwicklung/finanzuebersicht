import WidgetKit
import SwiftUI
import AppIntents

/// Shared with the MAUI host — must match Finanzuebersicht.Core.Constants.AppGroupIds.
enum AppGroup {
    static let id = "group.de.thomasmenzl.finanzuebersicht"
    static let pendingFileName = "quick-expense-pending.json"
    static let hasProKey = "hasPro"
    static let preferredLanguageKey = "preferredLanguage"
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

    /// In-app language from MAUI (`de` / `en`); nil → system locale.
    static var preferredLocale: Locale {
        if let code = UserDefaults(suiteName: AppGroup.id)?.string(forKey: AppGroup.preferredLanguageKey),
           !code.isEmpty {
            return Locale(identifier: code)
        }
        return .current
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

private enum L10n {
    static func string(_ key: String.LocalizationValue, locale: Locale = QuickExpenseSharedStore.preferredLocale) -> String {
        String(localized: key, locale: locale)
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
        let locale = QuickExpenseSharedStore.preferredLocale
        guard QuickExpenseSharedStore.hasPro else {
            return .result(dialog: "\(L10n.string("Finanzübersicht Pro is required for quick capture.", locale: locale))")
        }

        let trimmedAmount = amountText.trimmingCharacters(in: .whitespacesAndNewlines)
        let trimmedTitle = title.trimmingCharacters(in: .whitespacesAndNewlines)
        guard !trimmedAmount.isEmpty, !trimmedTitle.isEmpty else {
            return .result(dialog: "\(L10n.string("Amount and note are required.", locale: locale))")
        }

        try QuickExpenseSharedStore.enqueue(amountText: trimmedAmount, title: trimmedTitle)
        return .result(dialog: "\(L10n.string("Saved — open the app to sync.", locale: locale))")
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

    private var locale: Locale { QuickExpenseSharedStore.preferredLocale }

    private var coffeeTitle: String { L10n.string("Coffee", locale: locale) }
    private var snackTitle: String { L10n.string("Snack", locale: locale) }

    private var coffeeAmount: String {
        locale.language.languageCode?.identifier == "de" ? "3,50" : "3.50"
    }

    private var snackAmount: String {
        locale.language.languageCode?.identifier == "de" ? "5,00" : "5.00"
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            Text(L10n.string("Quick expense", locale: locale))
                .font(.headline)
            if entry.hasPro {
                Text(L10n.string("Tap a preset — open the app to sync", locale: locale))
                    .font(.caption)
                    .foregroundStyle(.secondary)
                HStack(spacing: 6) {
                    Button(intent: SaveQuickExpenseIntent(amountText: coffeeAmount, title: coffeeTitle)) {
                        Text(String(format: L10n.string("Coffee %@", locale: locale), coffeeAmount))
                            .font(.caption2)
                    }
                    .buttonStyle(.borderedProminent)
                    Button(intent: SaveQuickExpenseIntent(amountText: snackAmount, title: snackTitle)) {
                        Text(String(format: L10n.string("Snack %@", locale: locale), snackAmount))
                            .font(.caption2)
                    }
                    .buttonStyle(.bordered)
                }
            } else {
                Text(L10n.string("Pro unlocks Home Screen capture", locale: locale))
                    .font(.caption)
                    .foregroundStyle(.secondary)
            }
        }
        .environment(\.locale, locale)
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
        .configurationDisplayName(LocalizedStringResource("Quick expense"))
        .description(LocalizedStringResource("Capture a small expense without opening the full app."))
        .supportedFamilies([.systemSmall, .systemMedium])
    }
}
