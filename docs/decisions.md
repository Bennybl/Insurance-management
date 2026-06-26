# Project Decisions

This file records key implementation decisions made during development. The goal is to keep the project simple, reviewable, and easy to extend without adding unnecessary architecture.

## Keep a Layered Monolith

Decision:

```text
Use Controllers -> Services -> EF Core DbContext -> Database.
Do not introduce microservices, CQRS, or event-driven architecture.
```

Reason:

The challenge is scoped to a small backend API. A layered monolith gives clear separation of concerns while staying small enough to finish and review.

## Do Not Add a Repository Layer Yet

Decision:

```text
CustomerService uses AppDbContext directly.
No ICustomerRepository or repository abstraction is added for now.
```

Reason:

EF Core already provides repository-like behavior through `DbSet` and unit-of-work behavior through `DbContext`. Adding a repository now would mostly wrap EF calls without simplifying the code.

A repository layer may be useful later if:

```text
Queries become complex and shared.
Multiple services reuse the same persistence logic.
The project needs to isolate EF Core from application code.
Tests require mocked persistence instead of test databases.
```

For the current assignment, the simpler flow is preferred:

```text
Controller -> Service -> AppDbContext
```

## Use Services for Business Logic

Decision:

```text
CustomerService owns customer business behavior.
Controllers should stay thin and only handle HTTP concerns.
```

Reason:

This keeps the API layer simple and makes business rules easier to test later.

Examples:

```text
Reject duplicate customer email.
Deactivate customers instead of deleting them.
Reject deactivation when active policies exist.
```

## Use DTOs for API Contracts

Decision:

```text
Use request and response DTOs.
Do not expose EF entities directly from controllers.
```

Reason:

DTOs keep the API contract separate from database persistence models. This makes the API easier to evolve without forcing database-shaped responses.

## Keep the Domain Model Minimal

Decision:

```text
Customer does not include NationalId or DateOfBirth.
Policy does not include Currency or CoverageAmount.
Policy keeps PremiumAmount.
```

Reason:

The challenge does not require a strict schema. The model should be simple but still realistic.

Removed fields can be added later if real business requirements need them:

```text
NationalId: useful for identity/KYC flows.
DateOfBirth: useful for eligibility or pricing.
Currency: useful for multi-currency systems.
CoverageAmount: useful for product-specific coverage limits.
```

`PremiumAmount` remains because policy price is a useful basic policy detail.

## Keep PolicyProduct as a Lookup Table

Decision:

```text
PolicyProduct is a database lookup table.
Policy references it by ProductCode.
No product-management API is included yet.
```

Reason:

This keeps valid policy products controlled while avoiding a larger product-management module.

Initial products are seeded:

```text
CAR
HEALTH
LIFE
HOME
TRAVEL
```

Additional products can be inserted into the database later without changing the `Policy` table or C# entity model.

## Use DetailsJson as an Extension Point

Decision:

```text
Policy includes optional DetailsJson.
```

Reason:

Different insurance products may need different extra fields later. `DetailsJson` gives an extension point without building a complex polymorphic policy model now.

Examples:

```text
Car policy: license plate, vehicle model.
Health policy: coverage tier.
Life policy: beneficiary details.
```

## Use Navigation Properties

Decision:

```text
Policy keeps Customer and Product navigation properties.
Customer and PolicyProduct keep Policies collections.
```

Reason:

These are not separate database columns. They let EF Core understand relationships and make future queries easier.

The actual relationship fields are:

```text
Policy.CustomerId
Policy.ProductCode
```

## Do Not Use Sealed Classes

Decision:

```text
Use public class instead of public sealed class.
```

Reason:

`sealed` is valid, but it adds an extra concept that is not needed for this assignment. Plain classes are simpler and easier to read.

## Use CancellationToken in Async Service Methods

Decision:

```text
Async service methods accept CancellationToken cancellationToken = default.
```

Reason:

ASP.NET Core can cancel a request if the client disconnects, the request times out, or the application shuts down. Passing the token to EF Core lets database work stop when it is no longer needed.

Example:

```csharp
await dbContext.Customers.ToListAsync(cancellationToken);
```

The `= default` makes the parameter optional, so tests and internal calls can stay simple:

```csharp
await customerService.GetByIdAsync(id);
```

## Use Application Exceptions for Business Outcomes

Decision:

```text
Use NotFoundException for missing resources.
Use ConflictException for business rule conflicts.
```

Reason:

Services should not know about HTTP. They should express the business outcome. Controllers or global error handling can later translate these exceptions to HTTP responses.

Mapping planned for later:

```text
NotFoundException -> 404 Not Found
ConflictException -> 409 Conflict
```

Examples:

```text
Customer was not found.
A customer with this email already exists.
Customer cannot be deactivated while they have active policies.
```

## Use DataAnnotations for Request Validation

Decision:

```text
Use built-in DataAnnotations for simple DTO validation.
Use IValidatableObject for cross-field date validation.
Do not add FluentValidation yet.
```

Reason:

DataAnnotations are enough for the current scope and avoid adding another dependency.

Examples:

```text
Required fields.
Email format.
String length limits.
StartDate must be before EndDate.
```

## Do Not Validate Premium Range in DTOs

Decision:

```text
PremiumAmount remains on policy requests.
No Range attribute is currently applied to it.
```

Reason:

The validation was removed to keep DTO validation minimal. If needed, positive premium validation can be enforced later in `PolicyService` as a business rule.

## Use Branches Per Step

Decision:

```text
Each implementation step gets its own branch.
Each step is committed and pushed.
The user reviews and merges the PR before the next step.
```

Reason:

This keeps changes reviewable and makes the project history match the implementation plan.
