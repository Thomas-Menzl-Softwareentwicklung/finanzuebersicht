import XCTest

/// App Store screenshot automation for the MAUI iOS app.
/// Host app bundle id: `de.thomasmenzl.finanzuebersicht` (built by `dotnet build`, not this Xcode project).
final class FinanzuebersichtUITests: XCTestCase {

    private static let mauiBundleIdentifier = "de.thomasmenzl.finanzuebersicht"

    override func setUpWithError() throws {
        continueAfterFailure = false
    }

    @MainActor
    func testScreenshots() throws {
        let app = XCUIApplication(bundleIdentifier: Self.mauiBundleIdentifier)
        setupSnapshot(app)
        app.launchArguments += ["--screenshot-demo"]
        app.launch()

        let dashboard = app.descendants(matching: .any)["page.dashboard"]
        XCTAssertTrue(dashboard.waitForExistence(timeout: 30), "page.dashboard not found in accessibility tree")

        snapshot("01-dashboard")
    }
}
