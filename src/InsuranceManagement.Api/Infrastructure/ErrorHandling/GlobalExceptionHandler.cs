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

            DbUpdateException dbUpdateException when IsUniqueConstraintViolation(dbUpdateException, "Customers.Email") => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                "A customer with this email already exists."),

            DbUpdateException dbUpdateException when IsUniqueConstraintViolation(dbUpdateException, "Policies.PolicyNumber") => (
                StatusCodes.Status409Conflict,
                "Business rule conflict.",
                "A policy with this policy number already exists."),

            _ => (
                StatusCodes.Status500InternalServerError,
                "Unexpected server error.",
                "An unexpected error occurred.")
        };

        return new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path.ToString()
        };
    }

    private static bool IsUniqueConstraintViolation(DbUpdateException exception, string constraintTarget)
    {
        return exception.InnerException is SqliteException { SqliteErrorCode: 19 } sqliteException &&
            sqliteException.Message.Contains(constraintTarget, StringComparison.OrdinalIgnoreCase);
    }
}
