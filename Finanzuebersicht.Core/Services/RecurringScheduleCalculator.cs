using Finanzuebersicht.Models;

namespace Finanzuebersicht.Core.Services;

/// <summary>
/// Pure domain component for calculating recurring transaction schedules.
/// Single source of truth for: next-due calculation, exception application,
/// active-range checks, and occurrence-on-date/range queries. Used by
/// <see cref="RecurringGenerationService"/>, dashboard month forecast, and cashflow outlook.
/// </summary>
public static class RecurringScheduleCalculator
{
    /// <summary>
    /// Returns the next instance date after <paramref name="fromDate"/> based on
    /// the recurring transaction's interval and intervalFactor.
    /// </summary>
    public static DateTime GetNextInstance(RecurringTransaction recurring, DateTime fromDate)
    {
        var factor = Math.Max(1, recurring.IntervalFactor);
        return recurring.Interval switch
        {
            RecurrenceInterval.Daily => fromDate.Date.AddDays(1 * factor),
            RecurrenceInterval.Weekly => fromDate.Date.AddDays(7 * factor),
            RecurrenceInterval.Monthly => AddMonthsPreserveDay(fromDate.Date, 1 * factor),
            RecurrenceInterval.Quarterly => AddMonthsPreserveDay(fromDate.Date, 3 * factor),
            RecurrenceInterval.Yearly => AddMonthsPreserveDay(fromDate.Date, 12 * factor),
            _ => AddMonthsPreserveDay(fromDate.Date, 1 * factor),
        };
    }

    /// <summary>
    /// Applies any defined Skip or Shift exceptions to <paramref name="candidate"/>.
    /// Returns the effective date after applying exceptions.
    /// For a Skip, advances to the next instance. For a Shift with a ShiftToDate, returns that date.
    /// </summary>
    public static DateTime ApplyExceptions(RecurringTransaction recurring, DateTime candidate)
    {
        var adjusted = candidate.Date;

        if (recurring.Exceptions is null || recurring.Exceptions.Count == 0)
            return adjusted;

        // Loop to handle consecutive Skip exceptions (e.g. two back-to-back skipped dates).
        // 366 = max days in a leap year; handles the worst-case of a daily recurring with
        // a full year of consecutive Skips before finding a non-excepted date.
        const int maxIterations = 366;
        for (int i = 0; i < maxIterations; i++)
        {
            var exception = recurring.Exceptions.FirstOrDefault(e => e.InstanceDate.Date == adjusted);
            if (exception is null)
                return adjusted;

            switch (exception.Type)
            {
                case RecurringExceptionType.Skip:
                    adjusted = GetNextInstance(recurring, adjusted);
                    break;
                case RecurringExceptionType.Shift when exception.ShiftToDate.HasValue:
                    return exception.ShiftToDate.Value.Date;
                default:
                    return adjusted;
            }
        }

        return adjusted; // safety fallback
    }

    /// <summary>
    /// Returns true if <paramref name="referenceDate"/> falls within the active range of
    /// the recurring transaction (accounting for ReminderDaysBefore and Enddatum).
    /// </summary>
    public static bool IsWithinActiveRange(RecurringTransaction recurring, DateTime referenceDate)
    {
        var reference = referenceDate.Date;
        var start = recurring.Startdatum.Date;

        if (reference < start.AddDays(-recurring.ReminderDaysBefore))
            return false;

        if (recurring.Enddatum.HasValue && reference > recurring.Enddatum.Value.Date)
            return false;

        return true;
    }

    /// <summary>
    /// Calculates the next due date for a recurring transaction relative to
    /// <paramref name="referenceDate"/>, with exceptions applied.
    /// Returns null if the transaction is not active or outside its date range.
    /// </summary>
    public static DateTime? GetNextDueDate(RecurringTransaction recurring, DateTime referenceDate)
        => GetNextDueInstance(recurring, referenceDate)?.EffectiveDate;

    /// <summary>
    /// Returns the next schedule instance and its effective date (after Skip/Shift exceptions).
    /// </summary>
    public static (DateTime InstanceDate, DateTime EffectiveDate)? GetNextDueInstance(
        RecurringTransaction recurring,
        DateTime referenceDate)
    {
        if (!recurring.Aktiv || !IsWithinActiveRange(recurring, referenceDate))
            return null;

        var candidate = recurring.LetzteAusfuehrung.HasValue
            ? GetNextInstance(recurring, recurring.LetzteAusfuehrung.Value)
            : recurring.Startdatum.Date;

        while (candidate.Date < referenceDate.Date)
            candidate = GetNextInstance(recurring, candidate);

        var effective = ApplyExceptions(recurring, candidate);
        return (candidate.Date, effective);
    }

    /// <summary>
    /// Returns true if the recurring transaction has an effective occurrence on <paramref name="date"/>.
    /// </summary>
    public static bool OccursOnDate(RecurringTransaction recurring, DateTime date)
        => OccursInRange(recurring, date.Date, date.Date);

    /// <summary>
    /// Returns true if any effective occurrence falls within
    /// [<paramref name="rangeStart"/>, <paramref name="rangeEnd"/>] (inclusive).
    /// </summary>
    public static bool OccursInRange(RecurringTransaction recurring, DateTime rangeStart, DateTime rangeEnd)
    {
        if (!recurring.Aktiv)
            return false;

        if (recurring.Startdatum.Date > rangeEnd.Date)
            return false;

        if (recurring.Enddatum.HasValue && recurring.Enddatum.Value.Date < rangeStart.Date)
            return false;

        var candidate = recurring.LetzteAusfuehrung ?? recurring.Startdatum;
        if (candidate.Date < recurring.Startdatum.Date)
            candidate = recurring.Startdatum;

        while (candidate.Date <= rangeEnd.Date)
        {
            if (candidate.Date >= recurring.Startdatum.Date &&
                (!recurring.Enddatum.HasValue || candidate.Date <= recurring.Enddatum.Value.Date) &&
                TryGetEffectiveDate(recurring, candidate, out var effectiveDate) &&
                effectiveDate >= rangeStart.Date &&
                effectiveDate <= rangeEnd.Date)
            {
                return true;
            }

            var nextCandidate = GetNextInstance(recurring, candidate);
            if (nextCandidate.Date <= candidate.Date)
                return false;

            candidate = nextCandidate;
        }

        return false;
    }

    private static bool TryGetEffectiveDate(
        RecurringTransaction recurring,
        DateTime candidate,
        out DateTime effectiveDate)
    {
        effectiveDate = candidate.Date;

        var exception = recurring.Exceptions?.FirstOrDefault(e => e.InstanceDate.Date == candidate.Date);
        if (exception is null)
            return true;

        if (exception.Type == RecurringExceptionType.Skip)
            return false;

        if (exception.Type == RecurringExceptionType.Shift && exception.ShiftToDate.HasValue)
        {
            effectiveDate = exception.ShiftToDate.Value.Date;
            return true;
        }

        return true;
    }

    /// <summary>
    /// Adds months to a date while clamping the day to the last day of the target month.
    /// For example, Jan 31 + 1 month = Feb 28 (or 29 in a leap year).
    /// </summary>
    public static DateTime AddMonthsPreserveDay(DateTime date, int months)
    {
        var target = date.AddMonths(months);
        var day = date.Day;
        var daysInTarget = DateTime.DaysInMonth(target.Year, target.Month);
        if (day > daysInTarget)
            day = daysInTarget;
        return new DateTime(target.Year, target.Month, day);
    }
}
