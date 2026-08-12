import XCTest

/// App Store screenshot automation for the MAUI iOS app.
/// Host app bundle id: `de.thomasmenzl.finanzuebersicht` (built by `dotnet build`, not this Xcode project).
final class FinanzuebersichtUITests: XCTestCase {

    private static let mauiBundleIdentifier = "de.thomasmenzl.finanzuebersicht"
    private let elementTimeout: TimeInterval = 15

    /// Shell tab order in `AppShell.xaml` (locale-independent; ShellContent AutomationIds do not reach UITabBar on iOS).
    private enum Tab {
        static let dashboard = 0
        static let transactions = 1
        static let recurring = 2
        static let management = 3
        static let savings = 4
    }

    override func setUpWithError() throws {
        continueAfterFailure = false
    }

    // MARK: - Helpers

    private func element(_ id: String, in app: XCUIApplication) -> XCUIElement {
        app.descendants(matching: .any)[id]
    }

    @discardableResult
    private func waitFor(_ id: String, in app: XCUIApplication, timeout: TimeInterval? = nil) -> XCUIElement {
        let el = element(id, in: app)
        let wait = timeout ?? elementTimeout
        XCTAssertTrue(el.waitForExistence(timeout: wait), "\(id) not found in accessibility tree")
        return el
    }

    private func tapTab(_ index: Int, in app: XCUIApplication) {
        let tab = app.tabBars.buttons.element(boundBy: index)
        XCTAssertTrue(tab.waitForExistence(timeout: elementTimeout), "tab bar button at index \(index) not found")
        tab.tap()
    }

    private func tapSettings(in app: XCUIApplication) {
        let candidates = [
            app.navigationBars.buttons["toolbar.settings"],
            app.buttons["toolbar.settings"],
            element("toolbar.settings", in: app)
        ]
        let deadline = Date().addingTimeInterval(elementTimeout)
        while Date() < deadline {
            for candidate in candidates where candidate.exists {
                candidate.tap()
                return
            }
            RunLoop.current.run(until: Date().addingTimeInterval(0.2))
        }
        XCTFail("toolbar.settings not found in accessibility tree")
    }

    private func dismissQuickExpenseSheetIfPresent(in app: XCUIApplication) {
        let sheet = element("sheet.quick-expense", in: app)
        guard sheet.waitForExistence(timeout: 2) else { return }
        sheet.swipeDown()
        let gone = NSPredicate(format: "exists == false")
        let expectation = XCTNSPredicateExpectation(predicate: gone, object: sheet)
        _ = XCTWaiter.wait(for: [expectation], timeout: 5)
    }

    // MARK: - Screenshot flow

    @MainActor
    func testScreenshots() throws {
        let app = XCUIApplication(bundleIdentifier: Self.mauiBundleIdentifier)
        setupSnapshot(app)
        app.launchArguments += ["--screenshot-demo"]
        app.launch()

        waitFor("page.dashboard", in: app, timeout: 30)
        snapshot("01-dashboard")

        tapTab(Tab.transactions, in: app)
        waitFor("page.transactions", in: app)
        snapshot("02-transactions")

        tapTab(Tab.dashboard, in: app)
        waitFor("page.dashboard", in: app)
        waitFor("fab.schnell", in: app).tap()
        waitFor("sheet.quick-expense", in: app)
        snapshot("03-quick-expense")

        dismissQuickExpenseSheetIfPresent(in: app)
        tapTab(Tab.recurring, in: app)
        waitFor("page.recurring", in: app)
        snapshot("04-recurring")

        tapTab(Tab.management, in: app)
        waitFor("page.management", in: app)
        snapshot("05-management")

        tapTab(Tab.savings, in: app)
        waitFor("page.savings", in: app)
        snapshot("06-savings")

        tapSettings(in: app)
        waitFor("page.settings", in: app)
        snapshot("07-settings")
    }
}
