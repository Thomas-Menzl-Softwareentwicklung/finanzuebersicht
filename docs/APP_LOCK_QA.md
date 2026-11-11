# App-Sperre – Plattform- und Geräte-QA

Issue: [#407](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/407)

## Reviewkorrekturen – 2026-11-11

- Die Apple-Targets verwenden für die Sperrfrist `AppleContinuousTimeProvider` mit Darwins `CLOCK_MONOTONIC_RAW`. Diese monotone Nanosekunden-Uhr zählt Geräteschlaf mit; `TimeProvider.System` / `CLOCK_UPTIME_RAW` lässt Schlafzeit aus und ist dafür ungeeignet.
- Der native Mac-Escape-Pfad prüft die Zugriffsfreigabe innerhalb des tatsächlich ausgeführten UI-Callbacks. Eine bereits bestehende oder zwischen Tastendruck und Ausführung eingetretene Sperre kann das offene Formular dadurch nicht mehr verwerfen.
- Regressionen vor Korrektur nachgewiesen: 3 Escape-Tests fehlgeschlagen; beim bisherigen Apple-Uhrtyp 5 von 9 Uhrtests fehlgeschlagen. Danach vollständige Suite: 756 bestanden, 0 Fehler. Die Uhrtests modellieren Wach-/Schlafzeit getrennt, prüfen die Fristgrenzen und eine achtstündige Schlafphase; zusätzlich ist der native kontinuierliche Zeitstempel auf diesem Mac geprüft. Die Escape-Tests führen den echten Host-Bridge-Code mit ersetzten MAUI-App-/UI-Dispatcher-Grenzen aus.
- Buildprüfung der Korrekturen: Mac Catalyst x64 Release erfolgreich (lokal ad-hoc signiert, 0 Warnungen, 0 Fehler im abschließenden inkrementellen Lauf); iOS ARM64 Store Release/AOT erfolgreich (28 bekannte XAML-/Icon-Warnungen, 0 Fehler). Beim iOS-Prüfbuild waren IPA-Packaging und Archivierung deaktiviert.
- TestFlight-Bereitstellung: Anschließend wurde aus Commit `c1061c1` ein frisches iOS-Store-IPA für Version 1.26, Build 1.26.5 erstellt. App und Widget haben identische Versionsnummern, gültige Distribution-Signaturen und Store-Profile; kein `Platforms/`-Doppelpfad. Upload am 2026-11-11 erfolgreich, ASC-Verarbeitung `VALID` / `APP_STORE_ELIGIBLE`. Der Build enthält beide Reviewkorrekturen. Ein Mac-TestFlight-Upload ist damit nicht verbunden.
- Eine gezielte unabhängige Nachprüfung der Korrekturen ergab keine weiteren wesentlichen Befunde. Ein echter Schlaf-/Aufwach-Zyklus der ausgelieferten iOS-/Mac-App bleibt Teil der Geräte-QA; der deterministische Uhrtest ersetzt diesen Gerätetest nicht.

## Nachprüfung und Mac-Erweiterung – 2026-11-11

- iOS 1.26.2: Nutzer bestätigt nach dem Settings-Fix einen stabilen TestFlight-Praxistest. Das ersetzt keine Einzelabnahme aller unten aufgeführten Szenarien.
- Mac Catalyst registriert jetzt denselben `AppleDeviceAuthenticationService` wie iOS. `DeviceOwnerAuthentication` überlässt Touch ID und den macOS-Anmeldepasswort-Fallback dem Betriebssystem; eine eigene App-PIN wird nicht gespeichert. Die bestehenden Verzögerungen und der fensterbezogene Sichtschutz gelten weiter.
- Tests: 744 bestanden, 0 Fehler (`dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj --no-restore -m:1 -nodeReuse:false`).
- Mac Catalyst x64 Release: Build erfolgreich, 29 Warnungen, 0 Fehler. Neben den bekannten XAML-/Icon-Warnungen meldet das lokale Entwicklungsprofil MT7151 zu `get-task-allow`.
- Signierung: Apple-Identitäten sind außerhalb der Workspace-Sandbox vorhanden. Das installierte Mac-Entwicklungsprofil schließt diesen Mac aus; macOS verweigert deshalb den Start des unveränderten Entwicklungsbuilds. Für den UI-Test wurde ausschließlich eine separate Kopie unter `/private/tmp` ohne eingebettetes Profil und ohne profilgebundene iCloud-/Debug-Entitlements mit Apple Development signiert; die lokale Testkopie benötigt `com.apple.security.cs.allow-jit`. Die Projekt-Entitlements und die installierte App wurden dafür nicht geändert.
- Echter Mac-Laufzeittest: Testkopie startet, Settings öffnen sich, Systemauthentifizierung wird zur Aktivierung angefordert. Nutzer bestätigt erfolgreiche Authentifizierung mit dem macOS-Anmeldepasswort; der Passwort-Fallback ist damit lokal nachgewiesen. Beim erneuten Öffnen der Einstellungen ist die App-Sperre weiterhin aktiviert.
- Noch offen: Touch ID, Abbruch/Lockout, Kaltstart und Fristen auf dem Mac sowie ein sandboxed Mac-Store-/TestFlight-Build. Die lokale Testsignatur belegt diese Store-Konfiguration nicht.

## Historische Laufzeitumgebung der Erstumsetzung – 2026-11-09

| Prüfschritt | Ergebnis | Evidenz |
|---|---|---|
| Host | Intel-Mac, Darwin 25.6.0 | `uname -a`, 2026-11-09 |
| .NET / MAUI | SDK 10.0.103, Workload MAUI 10.0.20 | `dotnet workload list`, 2026-11-09 |
| Xcode | 26.6, Build 17F113 | `xcodebuild -version`, 2026-11-09 |
| Unit-/Integrationstests vor Änderungen | 690 bestanden, 0 Fehler | `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj -c Release`, 2026-11-09 |
| Windows 11 Gerät für Windows Hello Interop | Nicht vorhanden | Host ist macOS; Plattform-Spike nicht ausgeführt |
| Signiertes iOS-Gerät mit Face ID/Touch ID und Gerätecode | Nicht verbunden | Echter Fallback-Nachweis offen |
| Signierter Mac Catalyst Laufzeittest | Nicht ausgeführt | Simulator-/Buildnachweis ersetzt keinen signierten Laufzeittest |
| Code-Signing-Identitäten | 0 gültige Identitäten | `security find-identity -v -p codesigning`, 2026-11-09 |

## Freigabeumfang dieses Branches

| Plattform | Status | Verhalten |
|---|---|---|
| iOS | Implementiert, Geräteabnahme offen | `DeviceOwnerAuthentication`, Gerätecode-Fallback gemäß System-Policy; Vorschau-Schutz unabhängig von der App-Sperre |
| Mac Catalyst | Implementiert, lokaler signierter Systemdialog erfolgreich getestet | Touch ID / macOS-Anmeldepasswort gemäß System-Policy; Store-/TestFlight-Abnahme und weitere Gerätefälle offen |
| Windows | App-Sperre nicht verfügbar | Windows Hello und HWND-gebundener Vorschau-Schutz benötigen einen separaten Spike und Folge-PR |

## Akzeptanzprotokoll für Geräte

Die folgenden Punkte müssen auf einem echten Gerät vor Freigabe einzeln abgehakt werden. Ein erfolgreicher .NET-Build allein belegt kein Betriebssystemverhalten.

| Gerät / OS / Build | Szenario | Erwartetes Ergebnis | Status / Notiz |
|---|---|---|---|
| iPhone mit Face ID | Kaltstart, Face ID erfolgreich | Daten erst nach Authentifizierung sichtbar | Offen |
| iPhone mit Face ID | Biometrie nicht eingerichtet, Gerätecode vorhanden | iOS fordert Gerätecode über die Gerätebesitzer-Policy an | Offen |
| iPhone mit Touch ID | erfolgreicher Touch-ID-Versuch | Entsperrt | Offen |
| iPhone | Cancel, fehlgeschlagene Authentifizierung, Lockout | bleibt verdeckt; keine automatische Dialogschleife | Offen |
| iPhone | App-Switcher öffnen | Finanzdaten ab erstem Snapshot verdeckt, auch wenn App-Sperre ausgeschaltet ist | Offen |
| iPhone | jede Verzögerung, Hintergrundzeit unter/an/über Grenze | nach Einstellung und Regel `Dauer >= Frist` sperren | Offen |
| iPhone | Systemdialog/Kontrollzentrum/eigener Auth-Dialog | keine Fristverlängerung durch bloße Inaktivität; höchstens ein Systemdialog | Offen |
| iPhone | Deep Link nach Kaltstart | bleibt bis zur Entsperrung ausstehend, danach richtige Route | Offen |
| iPhone | offenes Formular oder Sheet | keine Daten sichtbar im Hintergrund; Zustand nach zulässigem Resume erhalten | Offen |
| Mac Catalyst | Touch ID, kein Touch ID, macOS-Passwort | tatsächliche `DeviceOwnerAuthentication`-Policy dokumentieren | macOS-Anmeldepasswort lokal erfolgreich bestätigt; Touch ID und Store-Test offen, siehe Nachprüfung |
| Windows 11 | Hello verfügbar / nicht verfügbar, Alt-Tab | Auth-Interop und Vorschau anhand HWND verifizieren | Nicht verfügbar; separates Plattformgerät/PR erforderlich |

## Historische Build-Evidenz der Erstumsetzung – 2026-11-09

| Prüfschritt | Ergebnis | Einschränkung |
|---|---|---|
| Mac Catalyst x64 Debug | Build erfolgreich, 28 Warnungen, 0 Fehler | MAUIG2045 zu Reflection-Fallbacks in XAML-Bindings und actool-Hinweise zu doppelten `appicon`-Setnamen; unsigned Debug; App-Sperre bleibt bis zum signierten Mac-Test deaktiviert |
| iOS Simulator x64 Debug | Build erfolgreich, 28 Warnungen, 0 Fehler | MAUIG2045 zu Reflection-Fallbacks in XAML-Bindings und actool-Hinweise zu doppelten `appicon`-Setnamen; unsigned Simulator-Build; kein Start: CoreSimulatorService am Host nicht verfügbar |
| Tests nach Implementierung | 730 bestanden, 0 Fehler | `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj -c Release --no-restore -m:1`, 2026-11-09 |
| Lokalisierte Face-ID-Nutzungstexte | In Root-`de.lproj` und `en.lproj` im iOS-Bundle vorhanden | Build-Bundle geprüft, 2026-11-09 |
| iOS-/Mac Catalyst Signierung | Keine Code-Signing-Identität am Host | Echter Gerätebuild und Systemdialog bleiben offen |
| iOS Simulator | CoreSimulatorService war für den Host nicht verfügbar | Kein Simulatorlauf; kein Face-ID-Code-Fallback-Nachweis |

## Offene Review-Abnahme

Die Geräteprüfung bleibt Merge-Voraussetzung: erster Frame und App-Switcher, Tastatur und Zubehör, offene Sheets und Modals, VoiceOver sowie mehrere Scenes/Fenster müssen auf echter Hardware geprüft werden. Schnelle Hintergrund-/Vordergrundwechsel einschließlich eines laufenden Systemdialogs und ein nach Gerätecode-Entfernung nicht verfügbarer Authentifizierungsversuch sind ebenfalls offen. Windows-Build und Windows-Spike bleiben mangels Windows-Gerät offen. Mac Catalyst hat den lokalen signierten Authentifizierungstest bestanden; die übrigen Mac-Szenarien und die Store-Konfiguration sind noch abzunehmen.

Für die Zustellung eines ausstehenden Deep-Links nach Freigabe gibt es noch keinen deterministischen Host-Integrationstest. Der Kaltstart-Deep-Link und der zuletzt ausstehende Link müssen bis zu einem solchen Test auf dem Gerät geprüft werden; Gate- und Coordinator-Tests ersetzen diese Prüfung nicht.
