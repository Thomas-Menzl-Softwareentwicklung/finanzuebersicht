import SwiftUI

/// Thin host app required by Xcode — never shipped with the MAUI app.
/// Only the QuickExpenseWidget extension target is embedded.
@main
struct WidgetHostApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}
