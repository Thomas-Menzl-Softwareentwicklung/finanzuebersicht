using Finanzuebersicht.Models;

namespace Finanzuebersicht.Tests.Core.Services;

public class RecurringScheduleCalculatorTests
{
    private static RecurringTransaction Make(
        RecurrenceInterval interval,
        int factor = 1,
        DateTime? start = null,
        DateTime? lastRun = null,
        DateTime? end = null,
        int reminderDays = 0,
        List<RecurringException>? exceptions = null) => new()
    {
        Id = "r1",
        Titel = "Test",
        Betrag = 10m,
        Interval = interval,
        IntervalFactor = factor,
        Startdatum = start ?? new DateTime(2025, 1, 1),
        LetzteAusfuehrung = lastRun,
        Enddatum = end,
        ReminderDaysBefore = reminderDays,
        Aktiv = true,
        Exceptions = exceptions ?? [],
    };

    // ── GetNextInstance ──────────────────────────────────────────────────────

    [Fact]
    public void GetNextInstance_Daily_AddsDays()
    {
        var r = Make(RecurrenceInterval.Daily);
        var result = RecurringScheduleCalculator.GetNextInstance(r, new DateTime(2025, 3, 1));
        Assert.Equal(new DateTime(2025, 3, 2), result);
    }

    [Fact]
    public void GetNextInstance_Daily_WithFactor2_Adds2Days()
    {
        var r = Make(RecurrenceInterval.Daily, factor: 2);
        var result = RecurringScheduleCalculator.GetNextInstance(r, new DateTime(2025, 3, 1));
        Assert.Equal(new DateTime(2025, 3, 3), result);
    }

    [Fact]
    public void GetNextInstance_Weekly_Adds7Days()
    {
        var r = Make(RecurrenceInterval.Weekly);
        var result = RecurringScheduleCalculator.GetNextInstance(r, new DateTime(2025, 3, 1));
        Assert.Equal(new DateTime(2025, 3, 8), result);
    }

    [Fact]
    public void GetNextInstance_Monthly_ClampsToEndOfMonth()
    {
        var r = Make(RecurrenceInterval.Monthly);
        // Jan 31 + 1 month = Feb 28 (2025, not a leap year)
        var result = RecurringScheduleCalculator.GetNextInstance(r, new DateTime(2025, 1, 31));
        Assert.Equal(new DateTime(2025, 2, 28), result);
    }

    [Fact]
    public void GetNextInstance_Quarterly_Adds3Months()
    {
        var r = Make(RecurrenceInterval.Quarterly);
        var result = RecurringScheduleCalculator.GetNextInstance(r, new DateTime(2025, 1, 15));
        Assert.Equal(new DateTime(2025, 4, 15), result);
    }

    [Fact]
    public void GetNextInstance_Yearly_Adds12Months()
    {
        var r = Make(RecurrenceInterval.Yearly);
        var result = RecurringScheduleCalculator.GetNextInstance(r, new DateTime(2025, 3, 1));
        Assert.Equal(new DateTime(2026, 3, 1), result);
    }

    // ── AddMonthsPreserveDay ─────────────────────────────────────────────────

    [Fact]
    public void AddMonthsPreserveDay_LeapYear_Feb29PlusOneYear()
    {
        var result = RecurringScheduleCalculator.AddMonthsPreserveDay(new DateTime(2024, 2, 29), 12);
        Assert.Equal(new DateTime(2025, 2, 28), result);
    }

    // ── ApplyExceptions ──────────────────────────────────────────────────────

    [Fact]
    public void ApplyExceptions_NoExceptions_ReturnsCandidate()
    {
        var r = Make(RecurrenceInterval.Monthly);
        var candidate = new DateTime(2025, 3, 1);
        Assert.Equal(candidate, RecurringScheduleCalculator.ApplyExceptions(r, candidate));
    }

    [Fact]
    public void ApplyExceptions_SkipException_AdvancesToNextInstance()
    {
        var candidate = new DateTime(2025, 3, 1);
        var r = Make(RecurrenceInterval.Monthly, exceptions:
        [
            new RecurringException { InstanceDate = candidate, Type = RecurringExceptionType.Skip }
        ]);
        var result = RecurringScheduleCalculator.ApplyExceptions(r, candidate);
        Assert.Equal(new DateTime(2025, 4, 1), result);
    }

    [Fact]
    public void ApplyExceptions_ShiftException_ReturnsShiftToDate()
    {
        var candidate = new DateTime(2025, 3, 1);
        var shiftTo = new DateTime(2025, 3, 5);
        var r = Make(RecurrenceInterval.Monthly, exceptions:
        [
            new RecurringException { InstanceDate = candidate, Type = RecurringExceptionType.Shift, ShiftToDate = shiftTo }
        ]);
        var result = RecurringScheduleCalculator.ApplyExceptions(r, candidate);
        Assert.Equal(shiftTo, result);
    }

    [Fact]
    public void ApplyExceptions_ExceptionOnDifferentDate_NotApplied()
    {
        var candidate = new DateTime(2025, 3, 1);
        var r = Make(RecurrenceInterval.Monthly, exceptions:
        [
            new RecurringException { InstanceDate = new DateTime(2025, 4, 1), Type = RecurringExceptionType.Skip }
        ]);
        Assert.Equal(candidate, RecurringScheduleCalculator.ApplyExceptions(r, candidate));
    }

    [Fact]
    public void ApplyExceptions_TwoConsecutiveSkips_SkipsBoth()
    {
        // March 1 → Skip, April 1 → Skip, May 1 → no exception → should return May 1
        var candidate = new DateTime(2025, 3, 1);
        var r = Make(RecurrenceInterval.Monthly, exceptions:
        [
            new RecurringException { InstanceDate = new DateTime(2025, 3, 1), Type = RecurringExceptionType.Skip },
            new RecurringException { InstanceDate = new DateTime(2025, 4, 1), Type = RecurringExceptionType.Skip }
        ]);
        var result = RecurringScheduleCalculator.ApplyExceptions(r, candidate);
        Assert.Equal(new DateTime(2025, 5, 1), result);
    }

    // ── IsWithinActiveRange ──────────────────────────────────────────────────

    [Fact]
    public void IsWithinActiveRange_BeforeStart_ReturnsFalse()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 6, 1));
        Assert.False(RecurringScheduleCalculator.IsWithinActiveRange(r, new DateTime(2025, 5, 1)));
    }

    [Fact]
    public void IsWithinActiveRange_OnStart_ReturnsTrue()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 6, 1));
        Assert.True(RecurringScheduleCalculator.IsWithinActiveRange(r, new DateTime(2025, 6, 1)));
    }

    [Fact]
    public void IsWithinActiveRange_AfterEnd_ReturnsFalse()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 1, 1), end: new DateTime(2025, 3, 31));
        Assert.False(RecurringScheduleCalculator.IsWithinActiveRange(r, new DateTime(2025, 4, 1)));
    }

    [Fact]
    public void IsWithinActiveRange_WithinReminderWindow_ReturnsTrue()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 6, 1), reminderDays: 5);
        // 3 days before start = still in reminder window
        Assert.True(RecurringScheduleCalculator.IsWithinActiveRange(r, new DateTime(2025, 5, 29)));
    }

    [Fact]
    public void IsWithinActiveRange_OutsideReminderWindow_ReturnsFalse()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 6, 1), reminderDays: 5);
        // 7 days before start = outside reminder window
        Assert.False(RecurringScheduleCalculator.IsWithinActiveRange(r, new DateTime(2025, 5, 25)));
    }

    // ── GetNextDueDate ───────────────────────────────────────────────────────

    [Fact]
    public void GetNextDueDate_NoLastRun_ReturnsStartDate()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 3, 1));
        var due = RecurringScheduleCalculator.GetNextDueDate(r, new DateTime(2025, 3, 1));
        Assert.Equal(new DateTime(2025, 3, 1), due);
    }

    [Fact]
    public void GetNextDueDate_WithLastRun_ReturnsNextAfterLastRun()
    {
        var r = Make(RecurrenceInterval.Monthly, lastRun: new DateTime(2025, 2, 1));
        var due = RecurringScheduleCalculator.GetNextDueDate(r, new DateTime(2025, 3, 1));
        Assert.Equal(new DateTime(2025, 3, 1), due);
    }

    [Fact]
    public void GetNextDueDate_InactiveRecurring_ReturnsNull()
    {
        var r = Make(RecurrenceInterval.Monthly);
        r.Aktiv = false;
        Assert.Null(RecurringScheduleCalculator.GetNextDueDate(r, DateTime.Today));
    }

    [Fact]
    public void GetNextDueDate_ExpiredRecurring_ReturnsNull()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2025, 1, 1), end: new DateTime(2025, 3, 31));
        Assert.Null(RecurringScheduleCalculator.GetNextDueDate(r, new DateTime(2025, 5, 1)));
    }

    // ── OccursOnDate / OccursInRange ─────────────────────────────────────────

    [Fact]
    public void OccursOnDate_MonthlyOnFifteenth_ReturnsTrueOnDueDay()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2026, 3, 15));
        Assert.True(RecurringScheduleCalculator.OccursOnDate(r, new DateTime(2026, 3, 15)));
        Assert.False(RecurringScheduleCalculator.OccursOnDate(r, new DateTime(2026, 3, 14)));
    }

    [Fact]
    public void OccursInRange_MonthlyRecurring_ReturnsTrueForMonthContainingDueDate()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2026, 1, 1));
        Assert.True(RecurringScheduleCalculator.OccursInRange(
            r,
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31)));
    }

    [Fact]
    public void OccursInRange_SkippedMonthInstance_ReturnsFalseForThatMonth()
    {
        var r = Make(RecurrenceInterval.Monthly, start: new DateTime(2026, 1, 1), exceptions:
        [
            new RecurringException
            {
                InstanceDate = new DateTime(2026, 3, 1),
                Type = RecurringExceptionType.Skip
            }
        ]);

        Assert.False(RecurringScheduleCalculator.OccursInRange(
            r,
            new DateTime(2026, 3, 1),
            new DateTime(2026, 3, 31)));
        Assert.True(RecurringScheduleCalculator.OccursInRange(
            r,
            new DateTime(2026, 4, 1),
            new DateTime(2026, 4, 30)));
    }

    [Fact]
    public void OccursOnDate_ShiftLaterMonthInstance_ReturnsTrueOnShiftTargetDate()
    {
        var r = Make(
            RecurrenceInterval.Monthly,
            start: new DateTime(2026, 1, 31),
            exceptions:
            [
                new RecurringException
                {
                    InstanceDate = new DateTime(2026, 2, 28),
                    Type = RecurringExceptionType.Shift,
                    ShiftToDate = new DateTime(2026, 3, 2)
                }
            ]);

        Assert.False(RecurringScheduleCalculator.OccursOnDate(r, new DateTime(2026, 2, 28)));
        Assert.True(RecurringScheduleCalculator.OccursOnDate(r, new DateTime(2026, 3, 2)));
    }

    [Fact]
    public void OccursInRange_AgreesWithDayByDayOccursOnDateScan()
    {
        var r = Make(
            RecurrenceInterval.Monthly,
            start: new DateTime(2026, 1, 31),
            exceptions:
            [
                new RecurringException
                {
                    InstanceDate = new DateTime(2026, 2, 28),
                    Type = RecurringExceptionType.Shift,
                    ShiftToDate = new DateTime(2026, 3, 2)
                }
            ]);

        var rangeStart = new DateTime(2026, 2, 1);
        var rangeEnd = new DateTime(2026, 3, 31);
        var dayScan = false;
        for (var day = rangeStart; day <= rangeEnd; day = day.AddDays(1))
        {
            if (RecurringScheduleCalculator.OccursOnDate(r, day))
            {
                dayScan = true;
                break;
            }
        }

        Assert.Equal(dayScan, RecurringScheduleCalculator.OccursInRange(r, rangeStart, rangeEnd));
    }
}
