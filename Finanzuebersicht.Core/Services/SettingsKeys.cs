namespace Finanzuebersicht.Core.Services
{
    /// <summary>
    /// Konstanten für App-Einstellungen.
    /// </summary>
    public static class SettingsKeys
    {
        // Darstellung und Lokalisierung
        /// <summary>
        /// Ausgewähltes App-Theme (System, Light oder Dark).
        /// </summary>
        public const string Theme = "Theme";

        /// <summary>
        /// Ausgewählter Sprachcode; leer bedeutet Systemsprache.
        /// </summary>
        public const string LanguageCode = "LanguageCode";

        /// <summary>
        /// Legacy-Key für den Anzeige-Währungscode.
        /// Neue Werte werden unter <see cref="DisplayCurrency"/> gespeichert.
        /// </summary>
        public const string Currency = "Currency";

        // Daten und Pfade
        /// <summary>
        /// Benutzerdefinierter Pfad für Datenspeicher.
        /// </summary>
        public const string DataPath = "DataPath";

        // Backup-Einstellungen
        /// <summary>
        /// Pfad zum Backup-Verzeichnis (Standard: {DataPath}/backups).
        /// </summary>
        public const string BackupPath = "BackupPath";

        /// <summary>
        /// Zeitstempel des letzten Backups (ISO 8601 Format).
        /// </summary>
        public const string LastBackupTime = "LastBackupTime";

        /// <summary>
        /// Ob automatische Backups aktiviert sind (true/false).
        /// </summary>
        public const string AutoBackupEnabled = "AutoBackupEnabled";

        /// <summary>
        /// Häufigkeit automatischer Backups: "daily", "weekly", "monthly".
        /// </summary>
        public const string BackupFrequency = "BackupFrequency";

        /// <summary>
        /// Maximale Anzahl aufzubewahrende Backups (Standard: 10).
        /// </summary>
        public const string MaxBackupsToKeep = "MaxBackupsToKeep";

        /// <summary>
        /// Whether the first-run onboarding wizard was completed (true/false).
        /// </summary>
        public const string OnboardingCompleted = "OnboardingCompleted";

        /// <summary>
        /// Whether the dashboard budget section is expanded (true/false).
        /// </summary>
        public const string DashboardBudgetExpanded = "DashboardBudgetExpanded";

        /// <summary>
        /// Whether the dashboard year details section is expanded (true/false).
        /// </summary>
        public const string DashboardYearDetailsExpanded = "DashboardYearDetailsExpanded";

        /// <summary>
        /// Whether the dashboard year month trend chart is expanded (true/false).
        /// </summary>
        public const string DashboardYearMonthTrendExpanded = "DashboardYearMonthTrendExpanded";

        /// <summary>
        /// Whether the dashboard year category breakdown is expanded (true/false).
        /// </summary>
        public const string DashboardYearCategoriesExpanded = "DashboardYearCategoriesExpanded";

        /// <summary>
        /// Whether the dashboard month expense categories section is expanded (true/false).
        /// </summary>
        public const string DashboardMonthExpensesExpanded = "DashboardMonthExpensesExpanded";

        /// <summary>
        /// Whether the dashboard month income categories section is expanded (true/false).
        /// </summary>
        public const string DashboardMonthIncomeExpanded = "DashboardMonthIncomeExpanded";

        /// <summary>
        /// Whether due recurring action cards are expanded on the dashboard (true/false).
        /// </summary>
        public const string DashboardDueDetailsExpanded = "DashboardDueDetailsExpanded";

        /// <summary>
        /// Whether the accounts overview section is expanded on the dashboard (true/false).
        /// </summary>
        public const string DashboardAccountsExpanded = "DashboardAccountsExpanded";

        /// <summary>
        /// Whether the cashflow preview section is expanded on the dashboard (true/false).
        /// </summary>
        public const string DashboardCashflowExpanded = "DashboardCashflowExpanded";

        /// <summary>
        /// Whether the account filter section is expanded on the dashboard (true/false).
        /// </summary>
        public const string DashboardFilterExpanded = "DashboardFilterExpanded";

        /// <summary>
        /// Whether compact insight rows are shown on the dashboard (true/false).
        /// </summary>
        public const string DashboardInsightRowsEnabled = "DashboardInsightRowsEnabled";

        /// <summary>
        /// ISO currency code for amount display formatting (EUR, CHF, USD, GBP).
        /// </summary>
        public const string DisplayCurrency = "DisplayCurrency";
    }
}
