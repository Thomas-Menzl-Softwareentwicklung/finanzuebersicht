using System;

namespace Finanzuebersicht.Core.Services
{
    public class TransactionDto
    {
        public DateTime Buchungsdatum { get; set; }
        public DateTime Wertstellung { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Zahlungspflichtige { get; set; } = string.Empty;
        public string Zahlungsempfaenger { get; set; } = string.Empty;
        public string Verwendungszweck { get; set; } = string.Empty;
        public string Umsatztyp { get; set; } = string.Empty;
        public string IBAN { get; set; } = string.Empty;
        public decimal Betrag { get; set; }
        public bool HasUnparsableAmount { get; set; }
        public string GlueubigerId { get; set; } = string.Empty;
        public string Mandatsreferenz { get; set; } = string.Empty;
        public string Kundenreferenz { get; set; } = string.Empty;
        // Optional: source account identifier (from file header)
        public string? SourceAccountId { get; set; }
    }
}
