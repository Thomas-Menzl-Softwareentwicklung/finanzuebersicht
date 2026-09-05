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
    private func waitForAnyMarker(_ markers: [String], in app: XCUIApplication, timeout: TimeInterval) -> Bool {
        let deadline = Date().addingTimeInterval(timeout)
        while Date() < deadline {
            for marker in markers {
                // Exact label match
                if app.staticTexts[marker].exists { return true }
                if app.descendants(matching: .any)[marker].exists { return true }

                // MAUI often exposes compound accessibility labels; match by substring.
                let predicate = NSPredicate(format: "label CONTAINS[c] %@", marker)
                let match = app.descendants(matching: .any).matching(predicate).firstMatch
                if match.exists { return true }
            }
            RunLoop.current.run(until: Date().addingTimeInterval(0.25))
        }
        return false
    }

    private func waitForSeededContent(in app: XCUIApplication) {
        // Allow async seed + DataChanged to settle (dashboard can appear before seed finishes).
        RunLoop.current.run(until: Date().addingTimeInterval(1.5))

        // Prefer markers that actually appear on Dashboard (categories / accounts).
        // Transaction titles like REWE live on the Transactions tab.
        let dashboardMarkers = [
            "Lebensmittel", "Wohnen", "Transport", "Girokonto", "Gehalt",
            "Groceries", "Housing", "Transport", "Checking", "Salary"
        ]
        if waitForAnyMarker(dashboardMarkers, in: app, timeout: 20) {
            return
        }

        // Fallback: open Transactions — titles are guaranteed there after a successful seed.
        tapTab(Tab.transactions, in: app)
        _ = waitFor("page.transactions", in: app)
        let transactionMarkers = [
            "REWE", "Gehalt", "Miete", "Netflix", "Café",
            "Salary", "Rent", "Grocery"
        ]
        XCTAssertTrue(
            waitForAnyMarker(transactionMarkers, in: app, timeout: 30),
            "Seeded fixture content not visible on Dashboard or Transactions"
        )
        tapTab(Tab.dashboard, in: app)
        _ = waitFor("page.dashboard", in: app)
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
        if !sheet.exists { return }

        // iPad form sheets often ignore a single swipe — try common dismiss controls.
        for title in ["Abbrechen", "Cancel", "Schließen", "Close", "Fertig", "Done"] {
            let button = app.buttons[title]
            if button.exists {
                button.tap()
                break
            }
        }

        if sheet.exists {
            app.swipeDown()
        }

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
        waitForSeededContent(in: app)
        snapshot("01-dashboard")

        tapTab(Tab.transactions, in: app)
        waitFor("page.transactions", in: app)
        snapshot("02-transactions")

        // Capture remaining tabs before opening the sheet so a stuck sheet cannot block iPad.
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

        // Quick-expense last (filename still 03-*).
        tapTab(Tab.dashboard, in: app)
        waitFor("page.dashboard", in: app)
        waitFor("fab.schnell", in: app).tap()
        waitFor("sheet.quick-expense", in: app)
        snapshot("03-quick-expense")
        dismissQuickExpenseSheetIfPresent(in: app)
    }
}