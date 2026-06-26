# Session Decisions

This file records only the main decisions made during the implementation session. The full implementation plan remains in `plan.md`.

## 1. Keep the Domain Model Minimal

We removed fields that were not required by the challenge scope:

```text
Customer: removed NationalId and DateOfBirth.
Policy: removed Currency and CoverageAmount.
Policy: kept PremiumAmount.
```

Reason: the challenge does not define a strict schema. The model should stay simple while still supporting customers, policies, and policy lifecycle behavior. Removed fields can be added later if real requirements need them.

## 2. Use PolicyProduct as a Lookup Table

`PolicyProduct` stays as a database lookup table with seeded initial values.

Reason: this keeps valid policy types controlled while allowing new products to be added later in the database without changing the `Policy` schema or C# entity model.

## 3. Do Not Add a Repository Layer Yet

`CustomerService` uses `AppDbContext` directly.

Reason: EF Core already provides repository-like behavior through `DbSet` and unit-of-work behavior through `DbContext`. Adding a repository now would mostly wrap EF calls without simplifying the project.

Current flow:

```text
Controller -> Service -> AppDbContext -> Database
```

## 4. Keep DTO Validation Simple

Use built-in `DataAnnotations` and `IValidatableObject` for simple request validation.

Reason: this is enough for the current scope and avoids adding another dependency like FluentValidation. We also removed the `Range` attribute from `PremiumAmount`; positive premium validation can be handled later in `PolicyService` if needed.

## 5. Use CancellationToken and Application Exceptions in Services

Service methods accept `CancellationToken cancellationToken = default` so request cancellation can flow into EF Core queries.

Services throw application exceptions for business outcomes:

```text
NotFoundException -> resource does not exist.
ConflictException -> valid request, but business rule conflict.
```

Reason: services should express business outcomes without directly returning HTTP responses. Global error handling maps these to `404` and `409`.

For duplicate customer emails, we rely on the database unique constraint. The global error handler converts the related `DbUpdateException` to a `409 Conflict` response instead of doing a separate pre-check query in `CustomerService`.
