using ErrorOr;
using FastEndpoints;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;

namespace DentFlow.Application.Common.Http;

/// <summary>
/// The one way an endpoint turns a handler's <see cref="Error"/> list into an HTTP response.
/// Calling the bare <c>SendErrorsAsync()</c> after <c>result.IsError</c> sent 400 with an
/// empty <c>errors[]</c> and turned NotFound/Conflict into 400 (QA-005).
/// </summary>
public static class ErrorOrEndpointExtensions
{
    public static int ToStatusCode(this ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        ErrorType.Unexpected => StatusCodes.Status500InternalServerError,
        _ => StatusCodes.Status400BadRequest,
    };

    /// <summary>
    /// Sends every error as <c>{ name: code, reason: description }</c>, with the status
    /// taken from the first error's type. Error metadata goes along as
    /// <c>{ name: "code.key", reason: value }</c>, so a client can read e.g. how many
    /// upcoming appointments blocked a deactivation without parsing the message.
    /// </summary>
    public static Task SendResultErrorsAsync(this IEndpoint endpoint, IReadOnlyList<Error> errors, CancellationToken ct)
    {
        foreach (var error in errors)
        {
            endpoint.ValidationFailures.Add(new ValidationFailure(error.Code, error.Description));
            foreach (var (key, value) in error.Metadata ?? [])
                endpoint.ValidationFailures.Add(new ValidationFailure(
                    $"{error.Code}.{key}", Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture)));
        }

        var status = errors.Count > 0 ? errors[0].Type.ToStatusCode() : StatusCodes.Status400BadRequest;
        return endpoint.HttpContext.Response.SendErrorsAsync(endpoint.ValidationFailures, status, cancellation: ct);
    }
}
