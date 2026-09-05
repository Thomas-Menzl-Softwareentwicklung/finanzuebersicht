import SwiftUI

/// Thin host app required by Xcode for the UI test bundle — never shipped with the MAUI app.
/// Screenshot tests launch `de.thomasmenzl.finanzuebersicht` (MAUI-built `.app`) via explicit bundle id.
@main
struct ScreenshotHostApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
        }
    }
}
