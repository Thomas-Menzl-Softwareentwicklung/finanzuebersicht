# Design: Import-Kategorisierung und variable Daueraufträge

**Status:** Entwurf zur Abstimmung  
**Reihenfolge:** 1. Import-Kategorisierung, 2. variable Daueraufträge

## Ziel und Anlass

Finanzübersicht soll Bankdateien so verarbeiten, dass wiederkehrende Zahlungspartner möglichst passend kategorisiert werden und Nutzer offene Fälle schnell korrigieren können. Die Erkennung bleibt lokal und nachvollziehbar; es werden keine Buchungsdaten an einen externen KI- oder Bankdienst übertragen.

Variable Kosten wie Energie, Telefon oder Abos sollen als Dauerauftrag planbar sein, ohne den Schätzwert mit einer bestätigten Buchung zu verwechseln.

## Bestehender Stand

- Der CSV-Import schlägt Kategorien über lokale Schlüsselwortregeln und historische Buchungen der letzten 24 Monate vor. Historische Treffer werden ab 50 % Übereinstimmung verwendet.
- Die Importvorschau erlaubt bereits die Kategorieänderung pro Zeile und filtert u. a. nach nicht kategorisierten, doppelten und ungültigen Zeilen.
- Kategorien werden während des Imports nicht als Gruppenvorgang bearbeitet. Die Erkennung zeigt dem Nutzer nicht klar, ob ein Vorschlag aus einer Regel oder aus der Buchungshistorie stammt.
- Daueraufträge werden mit festem Betrag geführt. `BookDueRecurringInstanceUseCase` akzeptiert bereits einen optionalen `amountOverride`, aber das Modell hat kein Variabel-Flag und der Dashboard-Flow verwendet den Override noch nicht. Die 30-Tage-Cashflow-Vorschau nutzt `RecurringTransaction.Betrag` als festen Wert. Die separate Monatsprognose basiert auf historischen Transaktionen und bleibt davon unabhängig.

## Ziele

### Import-Kategorisierung

- Vorschlagsquelle in der Vorschau erkennbar machen (Schlüsselwortregel, Buchungshistorie, kein Vorschlag).
- Gleichartige Zahlungspartner in der Vorschau gruppiert korrigieren können.
- Mehrere ausgewählte gültige, nicht doppelte Importzeilen in einem Vorgang kategorisieren können.
- Unklare Treffer nicht automatisch als verlässlich darstellen; Nutzerentscheidungen bleiben kontrollierbar.
- Nach erfolgreicher Korrektur sollen spätere Importe aus der bestehenden Buchungshistorie profitieren.

### Variable Daueraufträge

- Daueraufträge optional als betragsvariabel kennzeichnen.
- Den zuletzt gebuchten Betrag als vorausgefüllten Schätzwert verwenden; den beim Anlegen eingetragenen Betrag als Ausgangswert erhalten.
- Variable Posten in der 30-Tage-Cashflow-Vorschau sichtbar als Schätzung kennzeichnen.
- Beim Buchen den Betrag bestätigen oder ändern lassen; die Buchung verwendet ausschließlich den bestätigten Betrag.
- Ein neu bestätigter Betrag wird zur Schätzung für die nächste Fälligkeit. Bereits gebuchte Transaktionen bleiben unverändert.

## Nicht-Ziele

- Cloud- oder KI-basierte Kategorisierung, Bankanbindung oder Übertragung von Buchungsdaten an Dritte.
- Beliebige Massenänderungen von Datum, Betrag oder Löschen.
- Automatische Übernahme eines variablen Schätzwerts als gebuchte Transaktion ohne Bestätigung.
- Prognosemodelle für variable Beträge oder Erkennung von Ausreißern.

## Ansätze und Entscheidung

### 1. Import-Kategorisierung

1. **Nur Nachbearbeitung nach dem Commit:** einfacher eigener Bereich für unkategorisierte Buchungen, aber Nutzer muss importierte Einträge erneut aufsuchen und die Vorschlagslogik bleibt undurchsichtig.
2. **Nur bessere Vorschläge in der Vorschau:** bessere Erkennung, aber viele unklare Fälle müssen weiter einzeln bearbeitet werden.
3. **Hybrid (gewählt):** Vorschlagsquelle anzeigen, gleiche Zahlungspartner gruppieren und verbleibende oder falsch erkannte Einträge in der Vorschau gemeinsam korrigieren. Das verbindet erklärbare Vorschläge mit schneller manueller Kontrolle und passt zur bereits vorhandenen Vorschau.

### 2. Variable Daueraufträge

1. **Letzten Betrag als Schätzwert vorausfüllen:** gute Cashflow-Vorschau und schneller Buchungsablauf, aber die Vorschau muss den Schätzstatus deutlich zeigen.
2. **Keinen Betrag prognostizieren und Eingabe erst beim Buchen:** kein irreführender Schätzwert, aber schlechtere Cashflow-Vorschau.
3. **Gewählter Mittelweg:** letzten bestätigten Betrag vorausfüllen und in der Cashflow-Vorschau verwenden, ihn dort als Schätzung markieren und vor jeder Buchung bestätigen lassen. Dies wurde im Gespräch bestätigt.

## Vorgeschlagener Ablauf: Import-Kategorisierung

1. Die Vorschau zeigt weiterhin Duplikate und ungültige Zeilen wie heute.
2. Für importierbare Zeilen zeigt sie den Vorschlag sowie dessen Quelle an. Ohne Treffer bleibt die Zeile als nicht kategorisiert erkennbar.
3. Ein Gruppierungsmodus bündelt Zeilen mit demselben normalisierten Zahlungspartner und derselben vorgeschlagenen Kategorie. Nutzer kann die Gruppe einer Kategorie zuordnen.
4. Zusätzlich erlaubt Mehrfachauswahl, beliebige ausgewählte importierbare Zeilen einer Kategorie zuzuweisen.
5. Duplikate und ungültige Zeilen sind von Kategorisierungsaktionen ausgeschlossen. Kategorieänderungen sind vor dem Commit sichtbar und können erneut geändert werden.
6. Beim Commit wird das bestehende Modell benutzt; bestätigte Buchungen werden wie bisher zur Historie für spätere Vorschläge.

## Vorgeschlagener Ablauf: variable Daueraufträge

1. Im Dauerauftragsformular ergänzt ein Schalter „Betrag variiert“ die vorhandene Betragseingabe.
2. Der angelegte Betrag dient als initiale Schätzung, bis eine erste variable Instanz gebucht wurde.
3. Dashboard-Aktion „Buchen“ öffnet für variable Daueraufträge eine Betragseingabe, die mit der aktuellen Schätzung vorausgefüllt ist.
4. Bestätigung erzeugt die Transaktion mit dem eingegebenen Betrag und aktualisiert den Schätzwert für künftige Fälligkeiten.
5. Die 30-Tage-Cashflow-Vorschau verwendet den aktuellen Schätzwert und markiert den Eintrag als geschätzt. Die separate historische Monatsprognose bleibt unverändert. Feste Daueraufträge behalten ihr aktuelles Verhalten.
6. Überspringen, Verschieben und Ausnahmen bleiben unabhängig vom Betrag und folgen der bestehenden Logik.

## Akzeptanzkriterien

### Import-Kategorisierung

- Vorschau kennzeichnet die Quelle eines Vorschlags, sofern eine Regel oder historische Zuordnung greift.
- Mehrere gültige Importzeilen lassen sich gemeinsam kategorisieren.
- Gruppen gleicher Zahlungspartner lassen sich mit einer Kategorie korrigieren.
- Duplikate und ungültige Zeilen können nicht versehentlich mitkategorisiert oder importiert werden.
- Korrekturen sind vor dem Commit erkennbar und werden mit den importierten Transaktionen gespeichert.
- Vorhandener Import mit Mapping, Duplikaterkennung und Commit bleibt erhalten.

### Variable Daueraufträge

- Bei festen Daueraufträgen bleibt der Buchungsablauf unverändert.
- Variable Daueraufträge verlangen vor jeder Buchung eine Bestätigung/einen Betrag.
- Der bestätigte Betrag wird der Transaktion zugewiesen und zur nächsten Schätzung.
- Variable Schätzungen sind in der 30-Tage-Cashflow-Vorschau als solche erkennbar.
- Überspringen und Verschieben erzeugen keine Buchung und verändern den Betrag nicht.

## Umsetzungshinweise

- Für die Importseite bestehende `CsvImportOrchestrator`-Vorschläge und `ImportPreviewViewModel`-/`ImportPreviewRowItemViewModel`-Bearbeitung ausbauen; keine parallele Kategorisierungs-Pipeline einführen.
- Die aktuelle historische Zuordnung verwendet normalisierte Zahlungsempfänger und einen 50-%-Schwellwert. Änderungen an Matching-Regeln sollten separat anhand vorhandener Importfixtures überprüft werden.
- Für variable Daueraufträge lässt sich der vorhandene `amountOverride` in `BookDueRecurringInstanceUseCase` wiederverwenden; zusätzlich braucht es ein rückwärtskompatibles Flag und eine UI-Eingabe im Dashboard-Flow. Einen separaten optionalen `LastBookedAmount`-Wert am Dauerauftrag speichern, statt den ursprünglichen `Betrag` zu überschreiben. Die Cashflow-Vorschau verwendet `LastBookedAmount ?? Betrag` für variable Posten und ergänzt eine Schätzmarkierung (z. B. `IsEstimatedAmount`); `IsProjected` allein beschreibt bereits den geplanten Eintrag und unterscheidet keinen geschätzten Betrag.
- Datenmigration muss rückwärtskompatibel sein: bestehende Daueraufträge bleiben fest, wenn das neue Feld fehlt.
- UI-Texte Deutsch/Englisch, VoiceOver und Mac/Windows Desktop-Chrome mitdenken.

## Offene Implementierungsdetails

- Gruppierung nach exakter normalisierter Händlerkennung oder konservativer Ähnlichkeit: zunächst exakte Normalisierung bevorzugen, um falsche Zusammenfassungen zu vermeiden.
- Anzeige der Vorschlagsquelle in der Importvorschau: kurze Statusmarke mit zugänglicher Erläuterung.
- Für den variablen Betrag wird der letzte bestätigte Buchungsbetrag separat gespeichert; initial bleibt der beim Anlegen eingegebene Betrag die Schätzung.

