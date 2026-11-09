# App-Sperre – Plattform- und Geräte-QA

Issue: [#407](https://github.com/Thomas-Menzl-Softwareentwicklung/finanzuebersicht/issues/407)

## Laufzeitumgebung dieses Umsetzungszweigs

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
| Mac Catalyst | App-Sperre nicht verfügbar | Authentifizierungsadapter bleibt bis zu einem signierten Laufzeittest deaktiviert; Apple-Vorschau bleibt geschützt |
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
| Mac Catalyst | Touch ID, kein Touch ID, macOS-Passwort | tatsächliche `DeviceOwnerAuthentication`-Policy dokumentieren | Nicht freigegeben; Adapter ist bis zum signierten Laufzeittest deaktiviert |
| Windows 11 | Hello verfügbar / nicht verfügbar, Alt-Tab | Auth-Interop und Vorschau anhand HWND verifizieren | Nicht verfügbar; separates Plattformgerät/PR erforderlich |

## Build-Evidenz des Umsetzungszweigs

| Prüfschritt | Ergebnis | Einschränkung |
|---|---|---|
| Mac Catalyst x64 Debug | Build erfolgreich, 28 Warnungen, 0 Fehler | MAUIG2045 zu Reflection-Fallbacks in XAML-Bindings und actool-Hinweise zu doppelten `appicon`-Setnamen; unsigned Debug; App-Sperre bleibt bis zum signierten Mac-Test deaktiviert |
| iOS Simulator x64 Debug | Build erfolgreich, 28 Warnungen, 0 Fehler | MAUIG2045 zu Reflection-Fallbacks in XAML-Bindings und actool-Hinweise zu doppelten `appicon`-Setnamen; unsigned Simulator-Build; kein Start: CoreSimulatorService am Host nicht verfügbar |
| Tests nach Implementierung | 730 bestanden, 0 Fehler | `dotnet test Finanzuebersicht.Tests/Finanzuebersicht.Tests.csproj -c Release --no-restore -m:1`, 2026-11-09 |
| Lokalisierte Face-ID-Nutzungstexte | In Root-`de.lproj` und `en.lproj` im iOS-Bundle vorhanden | Build-Bundle geprüft, 2026-11-09 |
| iOS-/Mac Catalyst Signierung | Keine Code-Signing-Identität am Host | Echter Gerätebuild und Systemdialog bleiben offen |
| iOS Simulator | CoreSimulatorService war für den Host nicht verfügbar | Kein Simulatorlauf; kein Face-ID-Code-Fallback-Nachweis |

## Offene Review-Abnahme

Die Geräteprüfung bleibt Merge-Voraussetzung: erster Frame und App-Switcher, Tastatur und Zubehör, offene Sheets und Modals, VoiceOver sowie mehrere Scenes/Fenster müssen auf echter Hardware geprüft werden. Schnelle Hintergrund-/Vordergrundwechsel einschließlich eines laufenden Systemdialogs und ein nach Gerätecode-Entfernung nicht verfügbarer Authentifizierungsversuch sind ebenfalls offen. Windows-Build und Windows-Spike bleiben mangels Windows-Gerät offen; Mac-Catalyst-Authentifizierung bleibt nicht freigegeben.

Für die Zustellung eines ausstehenden Deep-Links nach Freigabe gibt es noch keinen deterministischen Host-Integrationstest. Der Kaltstart-Deep-Link und der zuletzt ausstehende Link müssen bis zu einem solchen Test auf dem Gerät geprüft werden; Gate- und Coordinator-Tests ersetzen diese Prüfung nicht.
