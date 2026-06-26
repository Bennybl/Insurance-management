using System.Text.Json;
using InsuranceManagement.Api.Application.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace InsuranceManagement.Api.Infrastructure.ErrorHandling;

public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var problem = CreateProblemDetails(httpContext, exception);

        if (problem.Status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception occurred.");
        }

        httpContext.Response.StatusCode = problem.Status ?? StatusCodes.Status500InternalServerError;
        httpContext.Response.ContentType = "application/problem+json";

        await httpContext.Response.WriteAsync(
            JsonSerializer.Serialize(problem, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            cancellationToken);

        return true;
    }

    private static ProblemDetails CreateProblemDetails(HttpContext httpContext, Exception exception)
    {
        var (statusCode, title, detail) = exception switch
        {
            NotFoundException notFoundException => (
                StatusCodes.Status404NotFound,
                "Resource not found.",
                notFoundException.Message),

            ConflictException conflictException => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                conflictException.Message),

            BadHttpRequestException badHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Bad request.",
                badHttpRequestException.Message),

            JsonException => (
                StatusCodes.Status400BadRequest,
                "Bad request.",
                "Request body contains invalid JSON."),

            DbUpdateConcurrencyException => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                "The requested change conflicts with the current data state."),

            DbUpdateException dbUpdateException when IsSqliteConstraintViolation(dbUpdateException, "Customers.Email") => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                "A customer with this email already exists."),

            DbUpdateException dbUpdateException when IsSqliteConstraintViolation(dbUpdateException, "Policies.PolicyNumber") => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                "A policy with this policy number already exists."),

            DbUpdateException dbUpdateException when IsSqliteConstraintViolation(dbUpdateException) => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                "The requested change violates a data integrity rule."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error.",
                "An unexpected error occurred.")
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.ToString()
        };

        problemDetails.Extensions["traceId"] = httpContext.TraceIdentifier;

        return problemDetails;
    }

    private static bool IsSqliteConstraintViolation(DbUpdateException exception, string? constraintTarget = null)
    {
        if (exception.InnerException is not SqliteException { SqliteErrorCode: 19 } sqliteException)
        {
            return false;
        }

        return string.IsNullOrWhiteSpace(constraintTarget) ||
            sqliteException.Message.Contains(constraintTarget, StringComparison.OrdinalIgnoreCase);
    }
}
