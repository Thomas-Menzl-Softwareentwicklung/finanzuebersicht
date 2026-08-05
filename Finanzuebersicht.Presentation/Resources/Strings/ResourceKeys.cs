namespace Finanzuebersicht.Resources.Strings;

/// <summary>
/// Type-safe constants for all localization resource keys.
/// </summary>
public static class ResourceKeys
{
    // Navigation
    public const string Nav_Dashboard = nameof(Nav_Dashboard);
    public const string Nav_Transaktionen = nameof(Nav_Transaktionen);
    public const string Nav_Dauerauftraege = nameof(Nav_Dauerauftraege);
    public const string Nav_Kategorien = nameof(Nav_Kategorien);
    public const string Nav_Verwaltung = nameof(Nav_Verwaltung);
    public const string Nav_Einstellungen = nameof(Nav_Einstellungen);
    public const string Nav_Cashflow = nameof(Nav_Cashflow);

    // Page Titles
    public const string Title_Transaktion = nameof(Title_Transaktion);
    public const string Title_Dauerauftrag = nameof(Title_Dauerauftrag);
    public const string Title_NeueKategorie = nameof(Title_NeueKategorie);
    public const string Title_KategorieBearbeiten = nameof(Title_KategorieBearbeiten);
    public const string Title_Jahresansicht = nameof(Title_Jahresansicht);
    public const string Title_Umbuchung = nameof(Title_Umbuchung);
    public const string Title_KontoHinzufuegen = nameof(Title_KontoHinzufuegen);
    public const string Title_KontoBearbeiten = nameof(Title_KontoBearbeiten);
    public const string Title_Cashflow = nameof(Title_Cashflow);

    // Labels
    public const string Lbl_Name = nameof(Lbl_Name);
    public const string Lbl_Icon = nameof(Lbl_Icon);
    public const string Lbl_Farbe = nameof(Lbl_Farbe);
    public const string Lbl_Typ = nameof(Lbl_Typ);
    public const string Lbl_Betrag = nameof(Lbl_Betrag);
    public const string Lbl_Titel = nameof(Lbl_Titel);
    public const string Lbl_Kategorie = nameof(Lbl_Kategorie);
    public const string Lbl_Datum = nameof(Lbl_Datum);
    public const string Lbl_Startdatum = nameof(Lbl_Startdatum);
    public const string Lbl_Enddatum = nameof(Lbl_Enddatum);
    public const string Lbl_EnddatumFestlegen = nameof(Lbl_EnddatumFestlegen);
    public const string Lbl_Aktiv = nameof(Lbl_Aktiv);
    public const string Lbl_Einnahme = nameof(Lbl_Einnahme);
    public const string Lbl_Ausgabe = nameof(Lbl_Ausgabe);
    public const string Lbl_Einnahmen = nameof(Lbl_Einnahmen);
    public const string Lbl_Ausgaben = nameof(Lbl_Ausgaben);
    public const string Lbl_Bilanz = nameof(Lbl_Bilanz);
    public const string Lbl_Monat = nameof(Lbl_Monat);
    public const string Lbl_Jahr = nameof(Lbl_Jahr);
    public const string Lbl_Prognose = nameof(Lbl_Prognose);
    public const string Lbl_AusgabenNachKategorie = nameof(Lbl_AusgabenNachKategorie);
    public const string Lbl_EinnahmenNachKategorie = nameof(Lbl_EinnahmenNachKategorie);
    public const string Lbl_Jahresausgaben = nameof(Lbl_Jahresausgaben);
    public const string Lbl_AusgabenProMonat = nameof(Lbl_AusgabenProMonat);
    public const string Lbl_Darstellung = nameof(Lbl_Darstellung);
    public const string Lbl_Speicherort = nameof(Lbl_Speicherort);
    public const string Lbl_Ueber = nameof(Lbl_Ueber);
    public const string Lbl_VerwendeteBibliotheken = nameof(Lbl_VerwendeteBibliotheken);
    public const string Lbl_Verwendungszweck = nameof(Lbl_Verwendungszweck);
    public const string Lbl_DatenWerdenGespeichert = nameof(Lbl_DatenWerdenGespeichert);
    public const string Lbl_Sprache = nameof(Lbl_Sprache);
    public const string Lbl_Anzeigewaehrung = nameof(Lbl_Anzeigewaehrung);
    public const string Lbl_KategorienColon = nameof(Lbl_KategorienColon);

    // Buttons
    public const string Btn_Speichern = nameof(Btn_Speichern);
    public const string Btn_Loeschen = nameof(Btn_Loeschen);
    public const string Btn_Abbrechen = nameof(Btn_Abbrechen);
    public const string Btn_OK = nameof(Btn_OK);
    public const string Btn_OrdnerWaehlen = nameof(Btn_OrdnerWaehlen);
    public const string Btn_Zuruecksetzen = nameof(Btn_Zuruecksetzen);
    public const string Btn_System = nameof(Btn_System);
    public const string Btn_Hell = nameof(Btn_Hell);
    public const string Btn_Dunkel = nameof(Btn_Dunkel);
    public const string Btn_Ja = nameof(Btn_Ja);
    public const string Btn_Nein = nameof(Btn_Nein);
    public const string Btn_AktivInaktiv = nameof(Btn_AktivInaktiv);
    public const string Btn_Duplizieren = nameof(Btn_Duplizieren);
    public const string Btn_Verwenden = nameof(Btn_Verwenden);
    public const string Btn_AlsVorlageSpeichern = nameof(Btn_AlsVorlageSpeichern);
    public const string Btn_Weiter = nameof(Btn_Weiter);
    public const string Btn_Fertig = nameof(Btn_Fertig);
    public const string Btn_Ueberspringen = nameof(Btn_Ueberspringen);
    public const string Btn_Zurueck = nameof(Btn_Zurueck);
    public const string Btn_Abgleichen = nameof(Btn_Abgleichen);
    public const string Btn_ErsteTransaktion = nameof(Btn_ErsteTransaktion);
    public const string Btn_ErsterDauerauftrag = nameof(Btn_ErsterDauerauftrag);

    // Hints / Placeholders
    public const string Hint_Kategoriename = nameof(Hint_Kategoriename);
    public const string Hint_Betrag = nameof(Hint_Betrag);
    public const string Hint_Beschreibung = nameof(Hint_Beschreibung);
    public const string Hint_Titelbeispiel = nameof(Hint_Titelbeispiel);
    public const string Hint_KategorieWaehlen = nameof(Hint_KategorieWaehlen);
    public const string Hint_iCloudTipp = nameof(Hint_iCloudTipp);

    // Empty States
    public const string Empty_KeineKategorien = nameof(Empty_KeineKategorien);
    public const string Empty_KeineDauerauftraege = nameof(Empty_KeineDauerauftraege);
    public const string Empty_KeineTransaktionen = nameof(Empty_KeineTransaktionen);
    public const string Empty_KeineSuchergebnisse = nameof(Empty_KeineSuchergebnisse);
    public const string Empty_KeineDashboardDaten = nameof(Empty_KeineDashboardDaten);
    public const string Empty_KeineJahresDaten = nameof(Empty_KeineJahresDaten);
    public const string Empty_NochKeineTransaktionen = nameof(Empty_NochKeineTransaktionen);
    public const string Empty_KeineKonten = nameof(Empty_KeineKonten);
    public const string Empty_KontenHinweis = nameof(Empty_KontenHinweis);
    public const string Btn_ZuTransaktionen = nameof(Btn_ZuTransaktionen);

    // Error Messages
    public const string Err_Titel = nameof(Err_Titel);
    public const string Err_UngueltigerBetrag = nameof(Err_UngueltigerBetrag);
    public const string Err_BetragGroesserNull = nameof(Err_BetragGroesserNull);
    public const string Err_TitelErforderlich = nameof(Err_TitelErforderlich);
    public const string Err_KategorieErforderlich = nameof(Err_KategorieErforderlich);
    public const string Err_SpeichernFehlgeschlagen = nameof(Err_SpeichernFehlgeschlagen);
    public const string Err_LoeschenFehlgeschlagen = nameof(Err_LoeschenFehlgeschlagen);
    public const string Err_LadenFehlgeschlagen = nameof(Err_LadenFehlgeschlagen);
    public const string Err_TransferKontenUnterschiedlich = nameof(Err_TransferKontenUnterschiedlich);
    public const string Err_UmbuchungNichtBearbeitbar = nameof(Err_UmbuchungNichtBearbeitbar);
    public const string Err_OrdnerNichtWaehlbar = nameof(Err_OrdnerNichtWaehlbar);
    public const string Err_SucheFehlgeschlagen = nameof(Err_SucheFehlgeschlagen);
    public const string Err_ProErforderlich = nameof(Err_ProErforderlich);
    public const string Err_LimitErreicht = nameof(Err_LimitErreicht);

    // Licensing
    public const string Lic_SectionTitle = nameof(Lic_SectionTitle);
    public const string Lic_ChannelDirect = nameof(Lic_ChannelDirect);
    public const string Lic_ChannelStore = nameof(Lic_ChannelStore);
    public const string Lic_TierFree = nameof(Lic_TierFree);
    public const string Lic_TierPro = nameof(Lic_TierPro);
    public const string Lic_SyncUnavailableDirect = nameof(Lic_SyncUnavailableDirect);
    public const string Lic_SyncInactive = nameof(Lic_SyncInactive);
    public const string Lic_SyncActive = nameof(Lic_SyncActive);
    public const string Lic_SyncEntitledComingSoon = nameof(Lic_SyncEntitledComingSoon);
    public const string Lic_DirectFullLocal = nameof(Lic_DirectFullLocal);
    public const string Lic_FreeLimitsHint = nameof(Lic_FreeLimitsHint);
    public const string Lic_StubHint = nameof(Lic_StubHint);
    public const string Lic_StubPro = nameof(Lic_StubPro);
    public const string Lic_StubSync = nameof(Lic_StubSync);
    public const string Lic_StubApply = nameof(Lic_StubApply);
    public const string Lic_BuyPro = nameof(Lic_BuyPro);
    public const string Lic_RestorePurchases = nameof(Lic_RestorePurchases);
    public const string Lic_PurchaseSuccess = nameof(Lic_PurchaseSuccess);
    public const string Lic_PurchaseFailed = nameof(Lic_PurchaseFailed);
    public const string Lic_RestoreSuccess = nameof(Lic_RestoreSuccess);
    public const string Lic_RestoreEmpty = nameof(Lic_RestoreEmpty);
    public const string Lic_SyncComingSoon = nameof(Lic_SyncComingSoon);
    public const string Lic_SyncPurchaseLater = nameof(Lic_SyncPurchaseLater);
    public const string Lic_UseStoreKit = nameof(Lic_UseStoreKit);

    // Confirmation Dialogs
    public const string Dlg_KategorieLoeschen = nameof(Dlg_KategorieLoeschen);
    public const string Dlg_KategorieLoeschenFrage = nameof(Dlg_KategorieLoeschenFrage);
    public const string Dlg_KontoLoeschen = nameof(Dlg_KontoLoeschen);
    public const string Dlg_KontoLoeschenFrage = nameof(Dlg_KontoLoeschenFrage);
    public const string Dlg_KontoArchivieren = nameof(Dlg_KontoArchivieren);
    public const string Dlg_KontoArchivierenFrage = nameof(Dlg_KontoArchivierenFrage);
    public const string Dlg_KontoReaktivieren = nameof(Dlg_KontoReaktivieren);
    public const string Dlg_KontoReaktivierenFrage = nameof(Dlg_KontoReaktivierenFrage);
    public const string Dlg_DauerauftragLoeschen = nameof(Dlg_DauerauftragLoeschen);
    public const string Dlg_DauerauftragLoeschenFrage = nameof(Dlg_DauerauftragLoeschenFrage);
    public const string Dlg_DauerauftragBuchen = nameof(Dlg_DauerauftragBuchen);
    public const string Dlg_DauerauftragBuchenFrage = nameof(Dlg_DauerauftragBuchenFrage);
    public const string Dlg_DauerauftragUeberspringen = nameof(Dlg_DauerauftragUeberspringen);
    public const string Dlg_DauerauftragUeberspringenFrage = nameof(Dlg_DauerauftragUeberspringenFrage);
    public const string Btn_AlsAusgefuehrtBuchen = nameof(Btn_AlsAusgefuehrtBuchen);
    public const string Btn_TerminUeberspringen = nameof(Btn_TerminUeberspringen);
    public const string Btn_TerminVerschieben = nameof(Btn_TerminVerschieben);
    public const string Dlg_TransaktionLoeschen = nameof(Dlg_TransaktionLoeschen);
    public const string Dlg_TransaktionLoeschenFrage = nameof(Dlg_TransaktionLoeschenFrage);
    public const string Dlg_UmbuchungLoeschen = nameof(Dlg_UmbuchungLoeschen);
    public const string Dlg_UmbuchungLoeschenFrage = nameof(Dlg_UmbuchungLoeschenFrage);
    public const string Dlg_VorlageLoeschen = nameof(Dlg_VorlageLoeschen);
    public const string Dlg_VorlageLoeschenFrage = nameof(Dlg_VorlageLoeschenFrage);
    public const string Dlg_SparZielLoeschen = nameof(Dlg_SparZielLoeschen);
    public const string Dlg_SparZielLoeschenFrage = nameof(Dlg_SparZielLoeschenFrage);

    // Settings Messages
    public const string Stn_UngueltigerOrdner = nameof(Stn_UngueltigerOrdner);
    public const string Stn_UngueltigerOrdnerDesc = nameof(Stn_UngueltigerOrdnerDesc);
    public const string Stn_SpeicherortGeaendert = nameof(Stn_SpeicherortGeaendert);
    public const string Stn_SpeicherortGeaendertDesc = nameof(Stn_SpeicherortGeaendertDesc);
    public const string Stn_SpeicherortZurueckgesetzt = nameof(Stn_SpeicherortZurueckgesetzt);
    public const string Stn_SpeicherortZurueckgesetztDesc = nameof(Stn_SpeicherortZurueckgesetztDesc);
    public const string Hint_SpeicherortNeustart = nameof(Hint_SpeicherortNeustart);
    public const string Stn_SpracheDeutsch = nameof(Stn_SpracheDeutsch);
    public const string Stn_SpracheEnglisch = nameof(Stn_SpracheEnglisch);
    public const string Stn_SpracheSystem = nameof(Stn_SpracheSystem);
    public const string Stn_CurrencyEur = nameof(Stn_CurrencyEur);
    public const string Stn_CurrencyChf = nameof(Stn_CurrencyChf);
    public const string Stn_CurrencyUsd = nameof(Stn_CurrencyUsd);
    public const string Stn_CurrencyGbp = nameof(Stn_CurrencyGbp);

    // Status
    public const string Status_Aktiv = nameof(Status_Aktiv);
    public const string Status_Inaktiv = nameof(Status_Inaktiv);

    // System Category Names
    public const string SysCat_Lebensmittel = nameof(SysCat_Lebensmittel);
    public const string SysCat_Transport = nameof(SysCat_Transport);
    public const string SysCat_Wohnen = nameof(SysCat_Wohnen);
    public const string SysCat_Unterhaltung = nameof(SysCat_Unterhaltung);
    public const string SysCat_Gesundheit = nameof(SysCat_Gesundheit);
    public const string SysCat_Gehalt = nameof(SysCat_Gehalt);
    public const string SysCat_Sonstiges = nameof(SysCat_Sonstiges);

    // Backup / Restore / Export
    public const string Stn_NoBackupYet = nameof(Stn_NoBackupYet);
    public const string Stn_LastBackupSeconds = nameof(Stn_LastBackupSeconds);
    public const string Stn_LastBackupMinutes = nameof(Stn_LastBackupMinutes);
    public const string Stn_LastBackupHours = nameof(Stn_LastBackupHours);
    public const string Stn_LastBackupDays = nameof(Stn_LastBackupDays);
    public const string Msg_BackupServiceNotAvailable = nameof(Msg_BackupServiceNotAvailable);
    public const string Msg_BackupSuccessTitle = nameof(Msg_BackupSuccessTitle);
    public const string Msg_BackupCreatedBody = nameof(Msg_BackupCreatedBody);
    public const string Msg_BackupFailedTitle = nameof(Msg_BackupFailedTitle);
    public const string Msg_NoBackupsTitle = nameof(Msg_NoBackupsTitle);
    public const string Msg_NoBackupsDesc = nameof(Msg_NoBackupsDesc);
    public const string Msg_AvailableBackupsTitle = nameof(Msg_AvailableBackupsTitle);
    public const string Msg_AndMoreBackups = nameof(Msg_AndMoreBackups);
    public const string Msg_RestoreConfirmTitle = nameof(Msg_RestoreConfirmTitle);
    public const string Msg_RestoreConfirmBody = nameof(Msg_RestoreConfirmBody);
    public const string Msg_RestoreSuccessTitle = nameof(Msg_RestoreSuccessTitle);
    public const string Msg_RestoreSuccessDesc = nameof(Msg_RestoreSuccessDesc);
    public const string Msg_RestoreFailedTitle = nameof(Msg_RestoreFailedTitle);
    public const string Msg_RestoreInconsistentTitle = nameof(Msg_RestoreInconsistentTitle);
    public const string Msg_RestoreInconsistentDesc = nameof(Msg_RestoreInconsistentDesc);
    public const string Msg_CSVExportedTitle = nameof(Msg_CSVExportedTitle);
    public const string Msg_CSVExportedBody = nameof(Msg_CSVExportedBody);
    public const string Msg_CSVExportFailedTitle = nameof(Msg_CSVExportFailedTitle);

    // Import messages
    public const string Msg_ImportFehlgeschlagen_Title = nameof(Msg_ImportFehlgeschlagen_Title);
    public const string Msg_ImportServiceNichtVerfuegbar = nameof(Msg_ImportServiceNichtVerfuegbar);
    public const string Msg_ImportAbgeschlossen_Title = nameof(Msg_ImportAbgeschlossen_Title);
    public const string Msg_ImportiertCount = nameof(Msg_ImportiertCount);
    public const string Msg_ImportFehler_Title = nameof(Msg_ImportFehler_Title);
    public const string Msg_ImportDuplikateCount = nameof(Msg_ImportDuplikateCount);
    public const string Msg_ImportFehlerCount = nameof(Msg_ImportFehlerCount);
    public const string Msg_ImportVorschauNichtVerfuegbar_Title = nameof(Msg_ImportVorschauNichtVerfuegbar_Title);
    public const string Msg_ImportVorschauNichtVerfuegbar_Body = nameof(Msg_ImportVorschauNichtVerfuegbar_Body);
    public const string Msg_ImportVorschauNichtsAusgewaehlt_Title = nameof(Msg_ImportVorschauNichtsAusgewaehlt_Title);
    public const string Msg_ImportVorschauNichtsAusgewaehlt_Body = nameof(Msg_ImportVorschauNichtsAusgewaehlt_Body);
    public const string Msg_VorlageGespeichert_Title = nameof(Msg_VorlageGespeichert_Title);
    public const string Msg_VorlageGespeichert_Body = nameof(Msg_VorlageGespeichert_Body);

    // Additional keys found during XAML scan
    public const string Lbl_OriginalDatum = nameof(Lbl_OriginalDatum);
    public const string Lbl_NeuesDatum = nameof(Lbl_NeuesDatum);
    public const string Hint_NotizOptional = nameof(Hint_NotizOptional);
    public const string Lbl_Intervall = nameof(Lbl_Intervall);
    public const string Lbl_IntervallFaktor = nameof(Lbl_IntervallFaktor);
    public const string Lbl_ErinnerungTage = nameof(Lbl_ErinnerungTage);
    public const string Lbl_Ausnahmen = nameof(Lbl_Ausnahmen);
    public const string Btn_SkipNextInstance = nameof(Btn_SkipNextInstance);
    public const string Btn_ShiftInstance = nameof(Btn_ShiftInstance);
    public const string Btn_BackupCreate = nameof(Btn_BackupCreate);
    public const string Btn_BackupManage = nameof(Btn_BackupManage);
    public const string Btn_BackupRestore = nameof(Btn_BackupRestore);
    public const string Btn_ExportCSV = nameof(Btn_ExportCSV);
    public const string Msg_BackupRestoreTitle = nameof(Msg_BackupRestoreTitle);
    public const string Msg_BackupRestoreDesc = nameof(Msg_BackupRestoreDesc);
    public const string Btn_Import = nameof(Btn_Import);
    public const string Btn_Filter = nameof(Btn_Filter);
    public const string Btn_Umbuchen = nameof(Btn_Umbuchen);
    public const string Btn_SchnellAusgabe = nameof(Btn_SchnellAusgabe);
    public const string Btn_UnkategorisiertFilter = nameof(Btn_UnkategorisiertFilter);
    public const string Btn_UnkategorisiertFilterZero = nameof(Btn_UnkategorisiertFilterZero);
    public const string Btn_FilterZuruecksetzen = nameof(Btn_FilterZuruecksetzen);
    public const string Title_ShiftInstance = nameof(Title_ShiftInstance);
    public const string Title_SchnellAusgabe = nameof(Title_SchnellAusgabe);
    public const string Lbl_SchnellAusgabeHinweis = nameof(Lbl_SchnellAusgabeHinweis);
    public const string Lbl_Info = nameof(Lbl_Info);
    public const string Hint_SchnellAusgabeInfo = nameof(Hint_SchnellAusgabeInfo);
    public const string Msg_SchnellAusgabeGespeichert = nameof(Msg_SchnellAusgabeGespeichert);
    public const string Msg_KeineUnkategorisierten = nameof(Msg_KeineUnkategorisierten);
    public const string A11y_SchnellAusgabe = nameof(A11y_SchnellAusgabe);

    // Additional common buttons / app info
    public const string Btn_Hinzufuegen = nameof(Btn_Hinzufuegen);
    public const string Btn_Prev = nameof(Btn_Prev);
    public const string Btn_Next = nameof(Btn_Next);
    public const string Title_AppName = nameof(Title_AppName);
    public const string Lbl_FrameworkInfo = nameof(Lbl_FrameworkInfo);
    public const string Icn_Recurring = nameof(Icn_Recurring);

    // Budgeting & Forecasting
    public const string Nav_SparZiele = nameof(Nav_SparZiele);
    public const string Title_SparZiele = nameof(Title_SparZiele);
    public const string Title_SparZielBearbeiten = nameof(Title_SparZielBearbeiten);
    public const string Btn_BeitragBuchen = nameof(Btn_BeitragBuchen);
    public const string Btn_Buchen = nameof(Btn_Buchen);
    public const string Lbl_Cashflow30Tage = nameof(Lbl_Cashflow30Tage);
    public const string Lbl_DashboardInsightRows = nameof(Lbl_DashboardInsightRows);
    public const string Lbl_Insights = nameof(Lbl_Insights);
    public const string Fmt_BudgetUeberLimitCount = nameof(Fmt_BudgetUeberLimitCount);
    public const string Lbl_CashflowNetto = nameof(Lbl_CashflowNetto);
    public const string Fmt_CashflowSummary = nameof(Fmt_CashflowSummary);
    public const string Fmt_SparZielFortschritt = nameof(Fmt_SparZielFortschritt);
    public const string Lbl_Jahresdetails = nameof(Lbl_Jahresdetails);
    public const string Lbl_MonatlichesBudget = nameof(Lbl_MonatlichesBudget);
    public const string Lbl_ZielBetrag = nameof(Lbl_ZielBetrag);
    public const string Lbl_AktuellerBetrag = nameof(Lbl_AktuellerBetrag);
    public const string Lbl_Faelligkeit = nameof(Lbl_Faelligkeit);
    public const string Lbl_Fortschritt = nameof(Lbl_Fortschritt);
    public const string Lbl_SparZiel = nameof(Lbl_SparZiel);
    public const string Hint_SparZielOptional = nameof(Hint_SparZielOptional);
    public const string Lbl_MonatlicheSparrate = nameof(Lbl_MonatlicheSparrate);
    public const string Lbl_VerknuepftAusBuchungen = nameof(Lbl_VerknuepftAusBuchungen);
    public const string Lbl_PrognoseZielerreichung = nameof(Lbl_PrognoseZielerreichung);
    public const string Lbl_Forecast = nameof(Lbl_Forecast);
    public const string Lbl_ForecastNaechsterMonat = nameof(Lbl_ForecastNaechsterMonat);
    public const string Lbl_SuchergebnisseAnzahl = nameof(Lbl_SuchergebnisseAnzahl);
    public const string Lbl_AlleKategorien = nameof(Lbl_AlleKategorien);
    public const string Lbl_AlleKonten = nameof(Lbl_AlleKonten);
    public const string Lbl_Kontosaldo = nameof(Lbl_Kontosaldo);
    public const string Lbl_Anfangssaldo = nameof(Lbl_Anfangssaldo);
    public const string Lbl_AnfangssaldoStichtag = nameof(Lbl_AnfangssaldoStichtag);
    public const string Hint_Anfangssaldo = nameof(Hint_Anfangssaldo);
    public const string Fmt_KontoSaldoAufschluesselung = nameof(Fmt_KontoSaldoAufschluesselung);
    public const string Lbl_Konten = nameof(Lbl_Konten);
    public const string Lbl_KontenUebersicht = nameof(Lbl_KontenUebersicht);
    public const string Lbl_Gesamtsaldo = nameof(Lbl_Gesamtsaldo);
    public const string Lbl_GesamtsaldoAktiv = nameof(Lbl_GesamtsaldoAktiv);
    public const string A11y_KontoUebersichtTippen = nameof(A11y_KontoUebersichtTippen);
    public const string Lbl_Archiviert = nameof(Lbl_Archiviert);
    public const string Lbl_VonKonto = nameof(Lbl_VonKonto);
    public const string Lbl_NachKonto = nameof(Lbl_NachKonto);
    public const string Btn_Archivieren = nameof(Btn_Archivieren);
    public const string Btn_Aktivieren = nameof(Btn_Aktivieren);
    public const string Hint_Systemkonto = nameof(Hint_Systemkonto);

    // Recurrence intervals
    public const string RecurrenceInterval_Daily = nameof(RecurrenceInterval_Daily);
    public const string RecurrenceInterval_Weekly = nameof(RecurrenceInterval_Weekly);
    public const string RecurrenceInterval_Monthly = nameof(RecurrenceInterval_Monthly);
    public const string RecurrenceInterval_Quarterly = nameof(RecurrenceInterval_Quarterly);
    public const string RecurrenceInterval_Yearly = nameof(RecurrenceInterval_Yearly);

    public const string AccountType_Girokonto = nameof(AccountType_Girokonto);
    public const string AccountType_Tagesgeld = nameof(AccountType_Tagesgeld);
    public const string AccountType_Kreditkarte = nameof(AccountType_Kreditkarte);
    public const string AccountType_Bargeld = nameof(AccountType_Bargeld);
    public const string AccountType_Depot = nameof(AccountType_Depot);
    public const string AccountType_Sonstiges = nameof(AccountType_Sonstiges);
    public const string Lbl_AlleTypen = nameof(Lbl_AlleTypen);
    public const string Lbl_VonDatum = nameof(Lbl_VonDatum);
    public const string Lbl_BisDatum = nameof(Lbl_BisDatum);
    public const string Hint_SucheTransaktionen = nameof(Hint_SucheTransaktionen);
    public const string Hint_IntervallWaehlen = nameof(Hint_IntervallWaehlen);
    public const string A11y_FilterUmschalten = nameof(A11y_FilterUmschalten);
    public const string Lbl_DauerauftraegeFaellig = nameof(Lbl_DauerauftraegeFaellig);
    public const string Lbl_DauerauftraegeFaellig_Singular = nameof(Lbl_DauerauftraegeFaellig_Singular);
    public const string Hint_HeuteFaellig = nameof(Hint_HeuteFaellig);
    public const string Hint_UeberfaelligSeitTagen = nameof(Hint_UeberfaelligSeitTagen);
    public const string Hint_FaelligInTagen = nameof(Hint_FaelligInTagen);
    public const string Lbl_OhneKategorie = nameof(Lbl_OhneKategorie);
    public const string Lbl_BudgetVon = nameof(Lbl_BudgetVon);
    public const string Lbl_Von = nameof(Lbl_Von);
    public const string Lbl_UeberBudget = nameof(Lbl_UeberBudget);
    public const string Lbl_BudgetHinweise = nameof(Lbl_BudgetHinweise);
    public const string Lbl_BudgetVerbraucht = nameof(Lbl_BudgetVerbraucht);
    public const string Lbl_TagesbudgetBisMonatsende = nameof(Lbl_TagesbudgetBisMonatsende);
    public const string Lbl_BudgetWarnung80 = nameof(Lbl_BudgetWarnung80);
    public const string Lbl_BudgetAufgebraucht = nameof(Lbl_BudgetAufgebraucht);
    public const string Lbl_HaeufigVerwendet = nameof(Lbl_HaeufigVerwendet);
    public const string Lbl_ImportFilterAlle = nameof(Lbl_ImportFilterAlle);
    public const string Lbl_ImportFilterBereit = nameof(Lbl_ImportFilterBereit);
    public const string Lbl_ImportFilterDuplikate = nameof(Lbl_ImportFilterDuplikate);
    public const string Lbl_ImportFilterProbleme = nameof(Lbl_ImportFilterProbleme);
    public const string Lbl_ImportFilterAusgeschlossen = nameof(Lbl_ImportFilterAusgeschlossen);
    public const string Lbl_ImportStatusBereit = nameof(Lbl_ImportStatusBereit);
    public const string Lbl_ImportStatusDuplikat = nameof(Lbl_ImportStatusDuplikat);
    public const string Lbl_ImportStatusUngueltig = nameof(Lbl_ImportStatusUngueltig);
    public const string Lbl_ImportStatusUnkategorisiert = nameof(Lbl_ImportStatusUnkategorisiert);
    public const string Lbl_ImportStatusSpeicherfehler = nameof(Lbl_ImportStatusSpeicherfehler);
    public const string Lbl_ImportStatusUnbekannt = nameof(Lbl_ImportStatusUnbekannt);
    public const string Lbl_ImportBereitCount = nameof(Lbl_ImportBereitCount);
    public const string Lbl_ImportDuplikateCount = nameof(Lbl_ImportDuplikateCount);
    public const string Lbl_ImportProblemeCount = nameof(Lbl_ImportProblemeCount);
    public const string Lbl_ImportAusgewaehltCount = nameof(Lbl_ImportAusgewaehltCount);
    public const string Msg_ImportStatusMissingDate = nameof(Msg_ImportStatusMissingDate);
    public const string Msg_ImportStatusPossibleDuplicate = nameof(Msg_ImportStatusPossibleDuplicate);
    public const string Msg_ImportStatusCategoryUnresolved = nameof(Msg_ImportStatusCategoryUnresolved);
    public const string Hint_BudgetOptional = nameof(Hint_BudgetOptional);
    public const string Hint_BudgetNull = nameof(Hint_BudgetNull);
    public const string Hint_ZielTitel = nameof(Hint_ZielTitel);
    public const string Empty_KeineSparZiele = nameof(Empty_KeineSparZiele);
    public const string Empty_KeinCashflow = nameof(Empty_KeinCashflow);
    public const string Btn_NeuesZiel = nameof(Btn_NeuesZiel);
    public const string Btn_ZielHinzufuegen = nameof(Btn_ZielHinzufuegen);

    // Backup-Liste
    public const string Ttl_BackupAuswaehlen = nameof(Ttl_BackupAuswaehlen);
    public const string Lbl_BackupTransaktionen = nameof(Lbl_BackupTransaktionen);
    public const string Lbl_BackupDatum = nameof(Lbl_BackupDatum);
    public const string Btn_BackupWiederherstellen = nameof(Btn_BackupWiederherstellen);
    public const string Empty_KeineBackupsVorhanden = nameof(Empty_KeineBackupsVorhanden);
    public const string Empty_ImportVorschauLeer = nameof(Empty_ImportVorschauLeer);
    public const string Empty_HintTransaktionen = nameof(Empty_HintTransaktionen);
    public const string Empty_HintDauerauftraege = nameof(Empty_HintDauerauftraege);
    public const string Empty_HintSparZiele = nameof(Empty_HintSparZiele);
    public const string Empty_HintCashflow = nameof(Empty_HintCashflow);
    public const string Empty_HintKategorien = nameof(Empty_HintKategorien);
    public const string Empty_HintBackups = nameof(Empty_HintBackups);
    public const string Lbl_Hilfe = nameof(Lbl_Hilfe);
    public const string Hint_EinrichtungWiederholen = nameof(Hint_EinrichtungWiederholen);
    public const string Btn_EinrichtungWiederholen = nameof(Btn_EinrichtungWiederholen);
    public const string Ttl_ImportVorschau = nameof(Ttl_ImportVorschau);
    public const string Msg_BackupRestoreConfirmTitle = nameof(Msg_BackupRestoreConfirmTitle);
    public const string Msg_BackupRestoreConfirmBody = nameof(Msg_BackupRestoreConfirmBody);

    // v1.14 — Feedback, Onboarding, Saldo-Abgleich
    public const string Msg_Gespeichert = nameof(Msg_Gespeichert);
    public const string Msg_Geloescht = nameof(Msg_Geloescht);
    public const string Btn_Rueckgaengig = nameof(Btn_Rueckgaengig);
    public const string Lbl_BerechneterSaldo = nameof(Lbl_BerechneterSaldo);
    public const string Lbl_IstSaldo = nameof(Lbl_IstSaldo);
    public const string Hint_SaldoAbgleich = nameof(Hint_SaldoAbgleich);
    public const string Msg_SaldoAbgeglichen = nameof(Msg_SaldoAbgeglichen);
    public const string Onboarding_PageTitle = nameof(Onboarding_PageTitle);
    public const string Onboarding_TitleWelcome = nameof(Onboarding_TitleWelcome);
    public const string Onboarding_TitleAccount = nameof(Onboarding_TitleAccount);
    public const string Onboarding_TitleFirstTransaction = nameof(Onboarding_TitleFirstTransaction);
    public const string Onboarding_TitleBackup = nameof(Onboarding_TitleBackup);
    public const string Onboarding_TitleDone = nameof(Onboarding_TitleDone);
    public const string Onboarding_DescWelcome = nameof(Onboarding_DescWelcome);
    public const string Onboarding_DescAccount = nameof(Onboarding_DescAccount);
    public const string Onboarding_DescFirstTransaction = nameof(Onboarding_DescFirstTransaction);
    public const string Onboarding_DescBackup = nameof(Onboarding_DescBackup);
    public const string Onboarding_DescDone = nameof(Onboarding_DescDone);
    public const string Onboarding_Btn_Anfangssaldo = nameof(Onboarding_Btn_Anfangssaldo);
    public const string Onboarding_Btn_ErsteTransaktion = nameof(Onboarding_Btn_ErsteTransaktion);
    public const string Onboarding_Btn_Einstellungen = nameof(Onboarding_Btn_Einstellungen);
    public const string Onboarding_Btn_ZumDashboard = nameof(Onboarding_Btn_ZumDashboard);

    // Accessibility
    public const string A11y_VorherigerMonat = nameof(A11y_VorherigerMonat);
    public const string A11y_NaechsterMonat = nameof(A11y_NaechsterMonat);
    public const string A11y_VorherigesJahr = nameof(A11y_VorherigesJahr);
    public const string A11y_NaechstesJahr = nameof(A11y_NaechstesJahr);
    public const string A11y_TransaktionHinzufuegen = nameof(A11y_TransaktionHinzufuegen);
    public const string A11y_UmbuchungHinzufuegen = nameof(A11y_UmbuchungHinzufuegen);
    public const string A11y_KategorieHinzufuegen = nameof(A11y_KategorieHinzufuegen);
    public const string A11y_KontoHinzufuegen = nameof(A11y_KontoHinzufuegen);
    public const string A11y_SparZielHinzufuegen = nameof(A11y_SparZielHinzufuegen);
    public const string A11y_DauerauftragHinzufuegen = nameof(A11y_DauerauftragHinzufuegen);
    public const string A11y_BudgetBalken = nameof(A11y_BudgetBalken);
    public const string A11y_SparZielBalken = nameof(A11y_SparZielBalken);
    public const string A11y_DauerauftraegeBannerHint = nameof(A11y_DauerauftraegeBannerHint);
    public const string A11y_HintBetrag = nameof(A11y_HintBetrag);
    public const string A11y_HintTitel = nameof(A11y_HintTitel);
    public const string A11y_HintBudget = nameof(A11y_HintBudget);
    public const string A11y_ChartLeer = nameof(A11y_ChartLeer);
    public const string A11y_ChartCategorySegment = nameof(A11y_ChartCategorySegment);
    public const string A11y_ChartUndWeitere = nameof(A11y_ChartUndWeitere);
    public const string A11y_ChartMonthSegment = nameof(A11y_ChartMonthSegment);
    public const string A11y_ChartForecastSegment = nameof(A11y_ChartForecastSegment);
    public const string A11y_TransaktionZeile = nameof(A11y_TransaktionZeile);
    public const string A11y_KategorieZeile = nameof(A11y_KategorieZeile);
    public const string A11y_KontoZeile = nameof(A11y_KontoZeile);
    public const string A11y_DauerauftragZeile = nameof(A11y_DauerauftragZeile);
    public const string A11y_SparZielZeile = nameof(A11y_SparZielZeile);
    public const string A11y_SelectionField = nameof(A11y_SelectionField);
    public const string A11y_SelectionFieldLeer = nameof(A11y_SelectionFieldLeer);
    public const string A11y_SegmentUmschalten = nameof(A11y_SegmentUmschalten);
    public const string A11y_CreateFormPanel = nameof(A11y_CreateFormPanel);
    public const string A11y_FormSheetDialog = nameof(A11y_FormSheetDialog);
}
