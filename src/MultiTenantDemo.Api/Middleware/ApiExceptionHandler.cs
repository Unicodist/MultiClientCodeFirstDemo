using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using MultiTenantDemo.Client.Domain;
using MultiTenantDemo.System.Domain;

namespace MultiTenantDemo.Api.Middleware;

internal sealed class ApiExceptionHandler : IExceptionHandler
{
    private readonly IProblemDetailsService _problemDetails;
    private readonly ILogger<ApiExceptionHandler> _logger;

    public ApiExceptionHandler(IProblemDetailsService problemDetails, ILogger<ApiExceptionHandler> logger)
    {
        _problemDetails = problemDetails;
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            TenantResolutionException { Reason: TenantResolutionFailure.Missing } => (StatusCodes.Status400BadRequest, "The X-Tenant-Id header is required."),
            TenantResolutionException { Reason: TenantResolutionFailure.Malformed } => (StatusCodes.Status400BadRequest, "The X-Tenant-Id header is malformed."),
            TenantResolutionException { Reason: TenantResolutionFailure.Unknown } => (StatusCodes.Status404NotFound, "Tenant not found."),
            TenantResolutionException { Reason: TenantResolutionFailure.Inactive } => (StatusCodes.Status403Forbidden, "Tenant is not active."),
            TenantResolutionException { Reason: TenantResolutionFailure.RegistryUnavailable } => (StatusCodes.Status503ServiceUnavailable, "Tenant registry is unavailable. Try again later."),
            TenantResolutionException { Reason: TenantResolutionFailure.InvalidConnectionMetadata } => (StatusCodes.Status500InternalServerError, "Tenant configuration error."),
            CustomerConflictException => (StatusCodes.Status409Conflict, "A customer with this email already exists."),
            _ => (0, string.Empty),
        };

        if (status == 0)
        {
            return false;
        }

        if (exception is TenantResolutionException tre)
        {
            _logger.LogWarning("Tenant resolution failed with {Reason}: {Message}", tre.Reason, tre.Message);
        }

        httpContext.Response.StatusCode = status;
        return await _problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = status, Title = title },
        }).ConfigureAwait(false);
    }
}
