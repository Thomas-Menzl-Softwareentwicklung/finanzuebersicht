using Finanzuebersicht.Models;

namespace Finanzuebersicht.Application.UseCases.Dashboard;

public class CashflowEntry
{
    public DateTime Date { get; set; }
    public string Title { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public TransactionType Typ { get; set; }
    public bool IsProjected { get; set; }
    public bool IsEstimatedAmount { get; set; }
    public bool IsOverdue { get; set; }
}

public class CashflowDayGroup
{
    public DateTime Date { get; set; }
    public List<CashflowEntry> Entries { get; set; } = [];
    public decimal NetAmount { get; set; }
    public bool IsNotable { get; set; }
}

public class CashflowOutlookData
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<CashflowDayGroup> Days { get; set; } = [];
    public decimal ProjectedIncome { get; set; }
    public decimal ProjectedExpenses { get; set; }
}
