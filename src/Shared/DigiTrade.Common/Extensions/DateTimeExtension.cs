using System.Diagnostics;
using System.Globalization;
using DigiTrade.Common.Models;

namespace DigiTrade.Common.Extensions;

public static class DateTimeExtension
{
    /// <summary>
    /// Javascript microtime difference from 1/1/1/ to 1/1/1970 will be:
    /// Math.abs(new Date(0, 0, 1).setFullYear(1)) = 62135607480000
    /// </summary>
    const long TicksDifferenceFromMicroTime = 62135607480000;

    /// <summary>
    /// Converts DateTime Ticks into JavaScript microtime,
    /// as ticks are recorded from 1/1/1.
    /// </summary>
    public static long TicksToMicrotime(this long ticks)
    {
        return ticks / 10000 - TicksDifferenceFromMicroTime;
    }

    /// <summary>
    /// Converts JavaScript microtime/timestamp (new Date().getTime())
    /// into .Net ticks (new DateTime().Ticks),
    /// as microtime is recorded from 1/1/1970.
    /// and ticks are recorded from 1/1/1.
    /// </summary>
    public static long MicrotimeToTicks(this long microtime)
    {
        return (microtime + TicksDifferenceFromMicroTime) * 10000;
    }

    /// <summary>
    /// Converts JavaScript microtime/timestamp into .Net DateTime,
    /// </summary>
    public static DateTime ToDateTime(this long microtime, DateTimeKind dateTimeKind = DateTimeKind.Utc)
    {
        var ticks = (microtime + TicksDifferenceFromMicroTime) * 10000;
        return new DateTime(ticks, dateTimeKind);
    }

    public static DateTimeOffset ToDateTimeOffset(this long microtime, DateTimeKind dateTimeKind = DateTimeKind.Utc)
    {
        var ticks = (microtime + TicksDifferenceFromMicroTime) * 10000;
        return new DateTimeOffset(ToDateTime(ticks, dateTimeKind));
    }

    public static DateTimeOffset ToDateTimeOffset(this long microtime, double timezoneHours = 0)
    {
        var ticks = (microtime + TicksDifferenceFromMicroTime) * 10000;
        return new DateTimeOffset(ticks, TimeSpan.FromHours(timezoneHours));
    }

    /// <summary>
    /// Strips seconds.
    /// </summary>
    public static DateTime StripSeconds(this DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            0);
    }

    /// <summary>
    /// Strip datetime milliseconds.
    /// </summary>
    public static DateTime StripMilliseconds(this DateTime dateTime)
    {
        return new DateTime(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second);
    }

    /// <summary>
    /// Strip datetimeoffset milliseconds.
    /// </summary>
    public static DateTimeOffset StripMilliseconds(this DateTimeOffset dateTime)
    {
        return new DateTimeOffset(
            dateTime.Year,
            dateTime.Month,
            dateTime.Day,
            dateTime.Hour,
            dateTime.Minute,
            dateTime.Second, TimeSpan.FromHours(0));
    }

    /// <summary>
    ///  Calculate difference between 2 dates in days
    /// </summary>
    public static double DiffDays(this DateTime start, DateTime end)
    {
        return start.Subtract(end).TotalDays;
    }

    /// <summary>
    /// Calculate difference between 2 dates in months with an
    /// average month of 30 days
    /// </summary>
    public static double DiffMonths(this DateTime start, DateTime end)
    {
        return start.Subtract(end).TotalDays / 30;
    }
    [DebuggerStepThrough]
    public static string Format(this DateTimeOffset date, string format = "yyyy-MM-dd HH:mm:ss.fffffffZ")
    {
        return date.ToString(format, CultureInfo.InvariantCulture);
    }
    public static double GetIntersectMillis(this Range<TimeSpan> a, Range<TimeSpan> b)
    {
        if(
            a.Start >= a.End ||
            b.Start >= b.End)
            throw new ArgumentException($"{nameof(Range<TimeSpan>)} is not a valid range.");
        if(a.End >= b.Start) return a.End.Subtract(b.Start).TotalMilliseconds;

        return -1;
    }
    public static double GetIntersectSeconds(this Range<TimeSpan> a, Range<TimeSpan> b)
    {
        if(
            a.Start >= a.End ||
            b.Start >= b.End)
            throw new ArgumentException($"{nameof(Range<TimeSpan>)} is not a valid range.");
        if(a.End >= b.Start) return a.End.Subtract(b.Start).TotalSeconds;

        return -1;
    }
    public static bool HasIntersection(this Range<TimeSpan> a, Range<TimeSpan> b) => GetIntersectSeconds(a, b) > 0;

    public static double GetIntersectMillis(this Range<DateTimeOffset> a, Range<DateTimeOffset> b)
    {
        if(
            a.Start >= a.End ||
            b.Start >= b.End)
            throw new ArgumentException($"{nameof(Range<DateTimeOffset>)} is not a valid range.");
        if(a.End >= b.Start) return a.End.Subtract(b.Start).TotalMilliseconds;

        return -1;
    }
    public static bool HasIntersection(this Range<DateTimeOffset> a, Range<DateTimeOffset> b) =>
        GetIntersectMillis(a, b) > 0;
    
    public static string HasValue(this DateTime? dateTime, string result)
    {
        return dateTime.HasValue && dateTime != DateTime.MinValue ? result : string.Empty;
    }
}