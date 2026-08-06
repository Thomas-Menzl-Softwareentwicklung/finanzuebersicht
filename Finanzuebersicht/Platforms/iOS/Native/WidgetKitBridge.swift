import Foundation
import WidgetKit

/// C ABI entry points for the MAUI host — WidgetKit APIs are Swift-only.
@_cdecl("finanzuebersicht_reload_all_widgets")
public func finanzuebersicht_reload_all_widgets() {
    WidgetCenter.shared.reloadAllTimelines()
    WidgetCenter.shared.reloadTimelines(ofKind: "QuickExpenseWidget")
}
