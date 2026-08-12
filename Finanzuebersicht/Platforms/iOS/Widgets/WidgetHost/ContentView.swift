import SwiftUI

struct ContentView: View {
    var body: some View {
        VStack(spacing: 12) {
            Image(systemName: "widget.small")
                .imageScale(.large)
                .foregroundStyle(.tint)
            Text("Widget Host")
                .font(.title2)
            Text("Build the .NET MAUI app (Finanzübersicht). This host exists only so Xcode can produce the Widget Extension.")
                .multilineTextAlignment(.center)
                .foregroundStyle(.secondary)
                .padding()
        }
        .padding()
    }
}

#Preview {
    ContentView()
}
