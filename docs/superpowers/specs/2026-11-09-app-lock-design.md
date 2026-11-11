# App-Sperre für Issue #407 – Design

Stand: 2026-11-09. Grundlage: [Issue #407](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/407) und die im Chat bestätigten Zeitoptionen. Dieses Dokument und der zugehörige Plan dienen dem Review vor der Implementierung.

## Ziel und vereinbartes Verhalten

Eine optionale, gerätelokale App-Sperre schützt die sichtbaren Finanzdaten. iOS verwendet Face ID/Touch ID mit Gerätecode-Fallback. Die native Systemauthentifizierung entscheidet über den Erfolg; die App speichert keinen eigenen PIN und keine biometrischen Daten. Die Einstellung steht unabhängig von Free/Pro und Store/Direct zur Verfügung.

- Standard einer neuen Installation: App-Sperre ausgeschaltet; beim Einschalten erfolgreiche Systemauthentifizierung verlangen.
- Bei eingeschalteter Sperre verlangt jeder neue Prozessstart eine Entsperrung, bevor Finanzdaten sichtbar oder bedienbar sind.
- Sperrverzögerung: **sofort, 1 Minute, 5 Minuten (Standard), 15 Minuten**.
- Die Zeit beginnt beim tatsächlichen Wechsel in den Hintergrund. Bei Rückkehr gilt `Hintergrunddauer >= Verzögerung` als abgelaufen. Jedes echte Verlassen startet eine neue Hintergrundfrist; die letzte Anmeldung ist kein absoluter Sitzungstimer.
- Innerhalb der Frist erscheint die bestehende Seite ohne neue Anmeldung; darüber erscheint zuerst die Sperransicht und dann der Systemdialog.
- Die Vorschau im App-Umschalter wird unabhängig von der Sperrverzögerung sofort mit einer vollständig deckenden, neutralen Ansicht geschützt. Auf Apple-Plattformen gilt dieser Vorschau-Schutz auch bei ausgeschalteter App-Sperre.
- Abbruch, Fehler, Sperrung der Biometrie und fehlende Verfügbarkeit öffnen die App nicht. Gerätecode-Fallback übernimmt das Betriebssystem. Nach Abbruch erscheint ein expliziter „Entsperren“-Button, keine Dialogschleife.
- Einstellungen zum Abschalten oder Verlängern der Frist erfordern erneut erfolgreiche Systemauthentifizierung. Verkürzen der Frist erfordert sie nicht.

Die nicht zuvor im Chat festgelegten Details (Opt-in, Einschaltprüfung, Änderungsprüfung, Grenzwert und gerätelokale Speicherung) sind technische Vorschläge dieses Designs.

## Bestehende Integration

Die App nutzt .NET 10 MAUI, Clean Architecture, MVVM und DE/EN-Ressourcen. `App.CreateWindow` erstellt unmittelbar `AppShell`. `App.OnStart` und `Window.Resumed` verarbeiten Initialisierung, Lizenz, wiederkehrende Buchungen, Widget-Inbox und Sync. Beide Apple-Targets haben bereits `MauiUISceneDelegate`-Ableitungen mit URL-Weiterleitung. Widget-Deep-Links öffnen die Schnellerfassung; Desktop-Menüs und Tastenkürzel können Aktionen direkt ausführen.

`ISettingsService` speichert JSON unter dem üblichen Datenpfad und behandelt Schreibfehler intern. Die Sperre bekommt deshalb einen eigenen, gerätelokalen Plattform-Einstellungsspeicher mit explizit erkennbarem Speicherfehler. App-Lock-Einstellungen werden weder den CloudKit-Entitäten noch dem Finanzdaten-Backup hinzugefügt. Plattform-Backups oder Neuinstallation können diese lokale Einstellung zurücksetzen; dies ist keine manipulationssichere Zugriffskontrolle auf Dateien.

## Ansatz und Alternativen

Empfehlung für die Implementierung: eine plattformunabhängig testbare Sitzung mit nativen Authentifizierungsadaptern und einer nativen Abdeckung pro Fenster. Apple-Targets verwenden `LAContext` und `DeviceOwnerAuthentication`; die native Abdeckung liegt auch über Shell-Modals. Bestehende Navigation und Formulare bleiben nach erfolgreicher Entsperrung erhalten.

Ein plattformübergreifendes Biometrie-Plugin würde etwas Adaptercode sparen, bringt aber eine weitere Abhängigkeit und muss Catalyst, Gerätecode-Fallback sowie Lifecycle-Verhalten gleichermaßen korrekt unterstützen. Eine normale modale MAUI-Sperrseite ist einfacher, kann aber selbst von Modals oder Fensterwechseln überlagert werden. Deshalb sind beide hier nicht der empfohlene Ansatz. Die technische Auswahl ist ein Vorschlag; die bereits vereinbarte Bedienlogik bleibt die Grundlage.

## Sitzung und Lifecycle

Zustände: `Disabled`, `Locked`, `Authenticating`, `Unlocked`. Sichtschutz ist zusätzlich ein eigener Zustand; auch eine entsperrte Sitzung kann während einer Unterbrechung verdeckt sein.

1. Beim Kaltstart wird der lokale Zustand vor der ersten sensiblen Darstellung geladen. Ladefehler führen bei unbekanntem Zustand zu einer gesperrten Fehleransicht, nicht zu einem stillen Reset.
2. `WillResignActive` blendet den Sichtschutz synchron ohne Animation ein. Ein Kontrollzentrum, ein Systemdialog oder der eigene Authentifizierungsdialog starten allein noch keinen Hintergrundtimer.
3. `DidEnterBackground` markiert die Frist, invalidiert laufende Authentifizierung und deren Ergebnisgeneration. Keine Dialoge im Hintergrund starten.
4. Bei erneuter Aktivierung wird zuerst der Sperrbedarf festgestellt. Erst nach einer erlaubten Sitzung wird die Abdeckung entfernt. Ein laufender Systemdialog darf durch die Aktivierung keinen zweiten Dialog auslösen.
5. Pro Sitzung läuft höchstens eine Authentifizierung. Nach Background/Dispose eintreffende Ergebnisse dürfen keinen Sichtschutz entfernen. Jeder neue Versuch erhält einen neuen nativen Kontext; Kontext nach Abschluss invalidieren und freigeben.
6. Zeitmessung während des Prozesses erfolgt monoton und einschließlich Geräteschlaf über `TimeProvider`, nicht über die veränderbare Systemuhr. Auf Apple-Plattformen wird dafür `CLOCK_MONOTONIC_RAW` verwendet; `TimeProvider.System` zählt dort Geräteschlaf nicht mit. Nach Prozessneustart gilt die Sperre unabhängig von einer früheren Frist.
7. Jeder Scene/Fenster-Host wird einzeln geschützt. Solange eine andere Scene sichtbar aktiv ist, beginnt kein globaler Hintergrundtimer; neu hinzukommende Fenster prüfen dieselbe Sitzung vor ihrer Darstellung.

Deep-Links werden bis nach erfolgreicher Entsperrung zurückgehalten (vorhandene Semantik: jüngster ausstehender Link). Menüaktionen, Tastenkürzel und Eingaben erreichen während der Sperre keine Finanzansicht. Lizenzprüfung und Datensync dürfen im Hintergrund weiterarbeiten, müssen den Sichtschutz respektieren. Ein gestarteter Dateidialog oder offenes Modal darf diesen Schutz nicht überdecken.

## Plattformumfang und Machbarkeitsgrenzen

**iOS:** verbindliches MVP, bestehende Mindestversion 15.0 beibehalten. `NSFaceIDUsageDescription` und lokalisierte InfoPlist-Strings ergänzen. Fähigkeit anhand der Gerätebesitzer-Policy prüfen; fehlende Biometrie allein darf ein Gerät mit nutzbarem Code nicht ausschließen. Auf echtem Face-ID-Gerät und Touch-ID-Gerät prüfen.

**Mac Catalyst:** bestehende Mindestversion 17.0 bleibt. Implementierung und Build des nativen Adapters belegen Kompilierbarkeit, aber nicht die verfügbare biometrische bzw. Passwortauthentifizierung in der ausgelieferten App. Die Fähigkeit wird erst nach einem signierten Catalyst-Test auf einem echten Mac freigegeben. Ist die Policy nicht vorhanden, ist Aktivierung mit Erklärung gesperrt. Bestehende Aktivierung niemals aufgrund eines temporären Fehlers still ausschalten.

**Windows:** in diesem Umsetzungszweig nicht freigeben, solange kein Windows-11-Testgerät vorhanden ist. WinUI-Desktop braucht einen HWND-bezogenen Verifikationsaufruf. Microsoft nennt für `IUserConsentVerifierInterop` Windows Build 22000; die App unterstützt derzeit ältere Windows-Versionen. Mindestversion nicht ungefragt erhöhen. Verfügbarkeit, OS-Version und App-Vorschau auf einem Windows-Gerät separat prüfen; ein MAUI-Overlay allein garantiert keine Abdeckung aller Windows-Systemvorschauen. Einen Windows-Hello-Folge-PR erst nach dem dokumentierten Plattformnachweis erstellen.

## Abgrenzung

Keine Dateiverschlüsselung (#53), keine Backup-/Sync-Verschlüsselung, kein eigener PIN, keine neue Account-Anmeldung. Kein Timer, der beim bloßen Lesen einer Seite im Vordergrund sperrt. Der vorhandene Widget-AppIntent kann weiterhin Ausgaben über die Inbox erfassen; die App-Sperre ist keine Sperre der Widget-Extension. Änderungen daran wären ein eigener Produktumfang. Screenshot-/Screen-Recording-Verhinderung ist nicht zugesagt.

## Erfolgskriterien und Aufwand

Die Implementierung ist erst abnahmefähig, wenn die Zeitgrenzen deterministisch getestet sind und ein echter iOS-Gerätetest Kaltstart, App-Wechsel, Systemdialog, Code-Fallback, Abbruch, Widget-Deep-Link und verdeckte Vorschau bestätigt. Formulare dürfen dabei keine Eingaben verlieren. Catalyst und Windows müssen als unterstützt oder nicht verfügbar korrekt dargestellt werden.

Planungsspanne: iOS und gemeinsamer Kern 2–4 Entwicklertage, Catalyst zusätzlich 1–3, Windows-Spike bis 0,5; Windows-Implementierung bei Erfolg zusätzlich 2–4. Geräte-/Signing-/Lifecycle-Probleme können diese Spanne erhöhen. Dies sind Schätzungen, keine gemessenen Durchlaufzeiten.

## Primärquellen

- [Apple: LAContext und NSFaceIDUsageDescription](https://developer.apple.com/documentation/localauthentication/lacontext)
- [Apple: Gerätebesitzer-Authentifizierung](https://developer.apple.com/documentation/localauthentication/lapolicy/deviceownerauthentication)
- [Apple: UI beim Wechsel in den Hintergrund und App-Snapshot](https://developer.apple.com/documentation/uikit/preparing-your-ui-to-run-in-the-background)
- [Microsoft: UserConsentVerifier für Desktop-Apps](https://learn.microsoft.com/en-us/uwp/api/windows.security.credentials.ui.userconsentverifier)
- [Microsoft: HWND-Interop und Mindestversion](https://learn.microsoft.com/en-us/windows/win32/api/userconsentverifierinterop/nf-userconsentverifierinterop-iuserconsentverifierinterop-requestverificationforwindowasync)
