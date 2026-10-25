namespace DentFlow.Application.Common.Http;

/// <summary>
/// DT-14: one rule for list paging. page=0 / -1 and pageSize=-1 reached Skip/Take
/// as negatives and ended in 500 on some lists while others clamped them.
/// </summary>
public static class Paging
{
    /// <summary>Page at least 1; size between 1 and <paramref name="max"/>, else the default.</summary>
    public static (int Page, int PageSize) Normalize(string? page, string? pageSize, int defaultSize, int max)
    {
        var p = int.TryParse(page, out var parsedPage) && parsedPage >= 1 ? parsedPage : 1;
        var size = int.TryParse(pageSize, out var parsedSize) && parsedSize >= 1 ? Math.Min(parsedSize, max) : defaultSize;
        return (p, size);
    }
}
