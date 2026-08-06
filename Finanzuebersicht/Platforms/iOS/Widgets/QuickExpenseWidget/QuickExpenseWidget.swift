import WidgetKit
import SwiftUI
import AppIntents

/// Shared with the MAUI host — must match Finanzuebersicht.Core.Constants.AppGroupIds.
enum AppGroup {
    static let id = "group.de.thomasmenzl.finanzuebersicht"
    static let pendingFileName = "quick-expense-pending.json"
    static let presetsFileName = "quick-expense-presets.json"
    static let hasProKey = "hasPro"
    static let preferredLanguageKey = "preferredLanguage"
    static let urlScheme = "finanzuebersicht"
}

struct PendingExpense: Codable, Identifiable {
    var id: String
    var amountText: String
    var title: String
    var createdAt: Date
}

struct WidgetPreset: Codable, Identifiable {
    var slot: Int
    var title: String
    var amountText: String

    var id: Int { slot }

    var isFilled: Bool {
        !title.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
            && !amountText.trimmingCharacters(in: .whitespacesAndNewlines).isEmpty
    }
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

    static var presetsURL: URL? {
        containerURL?.appendingPathComponent(AppGroup.presetsFileName)
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

    static func loadPresets() -> [WidgetPreset] {
        let seeded: [WidgetPreset] = [
            WidgetPreset(slot: 0, title: "Coffee", amountText: "3.50"),
            WidgetPreset(slot: 1, title: "Snack", amountText: "5.00"),
            WidgetPreset(slot: 2, title: "", amountText: ""),
            WidgetPreset(slot: 3, title: "", amountText: "")
        ]

        guard let url = presetsURL,
              FileManager.default.fileExists(atPath: url.path),
              let data = try? Data(contentsOf: url),
              let decoded = try? JSONDecoder().decode([WidgetPreset].self, from: data),
              !decoded.isEmpty else {
            return seeded.filter(\.isFilled)
        }

        return decoded
            .sorted { $0.slot < $1.slot }
            .filter(\.isFilled)
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

    static func adjustURL(amountText: String, title: String) -> URL? {
        var components = URLComponents()
        components.scheme = AppGroup.urlScheme
        components.host = "quick-expense"
        components.queryItems = [
            URLQueryItem(name: "amount", value: amountText),
            URLQueryItem(name: "title", value: title)
        ]
        return components.url
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
    private var languageCode: String {
        if let code = UserDefaults(suiteName: AppGroup.id)?.string(forKey: AppGroup.preferredLanguageKey),
           !code.isEmpty {
            return code
        }
        return Locale.current.language.languageCode?.identifier ?? "en"
    }

    func placeholder(in context: Context) -> QuickExpenseEntry {
        QuickExpenseEntry(
            date: Date(),
            hasPro: true,
            languageCode: languageCode,
            presets: [
                WidgetPreset(slot: 0, title: "Coffee", amountText: "3.50"),
                WidgetPreset(slot: 1, title: "Snack", amountText: "5.00")
            ])
    }

    func getSnapshot(in context: Context, completion: @escaping (QuickExpenseEntry) -> Void) {
        completion(makeEntry())
    }

    func getTimeline(in context: Context, completion: @escaping (Timeline<QuickExpenseEntry>) -> Void) {
        let entry = makeEntry()
        completion(Timeline(entries: [entry], policy: .after(Date().addingTimeInterval(15 * 60))))
    }

    private func makeEntry() -> QuickExpenseEntry {
        QuickExpenseEntry(
            date: Date(),
            hasPro: QuickExpenseSharedStore.hasPro,
            languageCode: languageCode,
            presets: QuickExpenseSharedStore.loadPresets())
    }
}

struct QuickExpenseEntry: TimelineEntry {
    let date: Date
    let hasPro: Bool
    let languageCode: String
    let presets: [WidgetPreset]
}

struct QuickExpenseWidgetEntryView: View {
    @Environment(\.widgetFamily) private var family
    var entry: QuickExpenseEntry

    private var locale: Locale {
        entry.languageCode.isEmpty
            ? QuickExpenseSharedStore.preferredLocale
            : Locale(identifier: entry.languageCode)
    }

    private var maxSlots: Int {
        family == .systemSmall ? 2 : 4
    }

    private var visiblePresets: [WidgetPreset] {
        Array(entry.presets.prefix(maxSlots))
    }

    var body: some View {
        VStack(alignment: .leading, spacing: 6) {
            Text(L10n.string("Quick expense", locale: locale))
                .font(.headline)
            if entry.hasPro {
                if visiblePresets.isEmpty {
                    Text(L10n.string("Configure shortcuts in Settings", locale: locale))
                        .font(.caption)
                        .foregroundStyle(.secondary)
                } else {
                    Text(L10n.string("Tap to book — pencil to edit", locale: locale))
                        .font(.caption2)
                        .foregroundStyle(.secondary)
                    ForEach(visiblePresets) { preset in
                        presetRow(preset)
                    }
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

    @ViewBuilder
    private func presetRow(_ preset: WidgetPreset) -> some View {
        let amountDisplay = displayAmount(preset.amountText)
        HStack(spacing: 4) {
            Button(intent: SaveQuickExpenseIntent(amountText: preset.amountText, title: preset.title)) {
                Text("\(preset.title) \(amountDisplay)")
                    .font(.caption2)
                    .lineLimit(1)
                    .frame(maxWidth: .infinity, alignment: .leading)
            }
            .buttonStyle(.borderedProminent)

            if let url = QuickExpenseSharedStore.adjustURL(amountText: preset.amountText, title: preset.title) {
                Link(destination: url) {
                    Image(systemName: "square.and.pencil")
                        .font(.caption)
                        .padding(6)
                }
                .accessibilityLabel(L10n.string("Edit amount", locale: locale))
            }
        }
    }

    private func displayAmount(_ invariant: String) -> String {
        guard let value = Decimal(string: invariant, locale: Locale(identifier: "en_US_POSIX")) else {
            return invariant
        }
        let formatter = NumberFormatter()
        formatter.locale = locale
        formatter.numberStyle = .decimal
        formatter.minimumFractionDigits = 2
        formatter.maximumFractionDigits = 2
        return formatter.string(from: value as NSDecimalNumber) ?? invariant
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
