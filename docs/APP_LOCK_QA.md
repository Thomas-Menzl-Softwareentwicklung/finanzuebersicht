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
| Mac Catalyst | Touch ID, kein Touch ID, macOS-Passwort | tatsächliche `DeviceOwnerAuthentication`-Policy dokumentieren | Offen |
| Windows 11 | Hello verfügbar / nicht verfügbar, Alt-Tab | Auth-Interop und Vorschau anhand HWND verifizieren | Offen; separates Plattformgerät/PR erforderlich |
