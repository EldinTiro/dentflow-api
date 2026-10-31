using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace DentFlow.API.Middleware;

/// <summary>
/// Turns PostgreSQL's "value too long for type character varying(n)" (22001) into a 400.
/// Many text fields have a column limit but no length rule in their validator, and the
/// database refusal then surfaced as a 500 (QA pass 2026-10-31, N-01). The text came from the
/// caller, so it is the caller's error — on every endpoint, including future ones. A validator
/// with an explicit MaximumLength still gives the more precise message where one exists.
/// Nothing is stored: the failing SaveChanges rolls the statement back.
/// </summary>
public sealed class ValueTooLongMiddleware(RequestDelegate next)
{
    public const string ErrorCode = "Validation.TooLong";
    public const string ErrorMessage = "One of the texts is too long. Please shorten it and try again.";

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (DbUpdateException ex) when (IsValueTooLong(ex) && !context.Response.HasStarted)
        {
            context.Response.Clear();
            context.Response.StatusCode = StatusCodes.Status400BadRequest;
            await context.Response.WriteAsJsonAsync(new
            {
                status = StatusCodes.Status400BadRequest,
                errors = new[] { new { name = ErrorCode, reason = ErrorMessage } },
            }, context.RequestAborted);
        }
    }

    private static bool IsValueTooLong(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.StringDataRightTruncation };
}
