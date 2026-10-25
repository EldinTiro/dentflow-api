using System.Globalization;

namespace DentFlow.Application.Common.Http;

/// <summary>
/// Parses a list filter bound. A full ISO instant (<c>2026-10-21T22:00:00Z</c>) is used
/// as-is — that is what the web client sends, the UTC bounds of the user's local day.
/// A plain date (<c>2026-10-22</c>) keeps its old meaning: that UTC calendar day.
/// Calendar dates alone cannot express a clinic's day off UTC (QA-008).
/// </summary>
public static class DateRangeBound
{
    /// <summary>
    /// DT-14: like <see cref="Parse"/>, but tells "no filter" (empty) from "unreadable" so an
    /// endpoint can answer 400 instead of silently dropping the filter.
    /// </summary>
    public static bool TryParse(string? value, bool endOfDay, out DateTime? bound)
    {
        bound = Parse(value, endOfDay);
        return bound is not null || string.IsNullOrWhiteSpace(value);
    }

    public static DateTime? Parse(string? value, bool endOfDay)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        if (DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date))
            return date.ToDateTime(endOfDay ? TimeOnly.MaxValue : TimeOnly.MinValue, DateTimeKind.Utc);

        if (DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out var instant))
            return instant.UtcDateTime;

        return null;
    }
}
