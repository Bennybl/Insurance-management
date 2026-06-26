# Session Decisions

This file records decisions made during the implementation session. It is not the full architecture plan; the main implementation plan remains in `plan.md`.

## Keep the Domain Model Minimal

Decision:

```text
Remove NationalId and DateOfBirth from Customer.
Remove Currency and CoverageAmount from Policy.
Keep PremiumAmount on Policy.
```

Reason:

The challenge document does not require a strict schema. We chose a simpler model that still supports the core customer-policy relationship.

Fields can be added later when real requirements justify them:

```text
NationalId: identity/KYC flows.
DateOfBirth: eligibility or pricing.
Currency: multi-currency support.
CoverageAmount: product-specific coverage limits.
```

`PremiumAmount` stays because the price of the policy is a useful basic policy detail.

## Keep PolicyProduct Extensible Through the Database

Decision:

```text
PolicyProduct remains a lookup table.
Initial products are seeded in EF configuration.
No product-management API is added now.
```

Reason:

This keeps the assignment focused while still allowing new products to be added later without changing the `Policy` table or C# entity model.

Example:

```sql
INSERT INTO PolicyProducts (Code, Name, IsActive)
VALUES ('PET', 'Pet Insurance', 1);
```

## Keep Navigation Properties

Decision:

```text
Keep Policy.Customer and Policy.Product navigation properties.
Keep Customer.Policies and PolicyProduct.Policies collections.
```

Reason:

These properties are not extra database columns. They help EF Core understand relationships and make future queries easier.

The actual relationship fields are:

```text
Policy.CustomerId
Policy.ProductCode
```

## Use Plain Classes Instead of Sealed Classes

Decision:

```text
Use public class instead of public sealed class.
```

Reason:

`sealed` is valid, but it adds an extra concept that is not needed here. Plain classes are simpler and easier to read for this assignment.

## Keep DTO Validation Simple

Decision:

```text
Use built-in DataAnnotations for simple request validation.
Use IValidatableObject only for cross-field date validation.
Do not add FluentValidation yet.
```

Reason:

The current validation needs are small. DataAnnotations cover required fields, email format, phone format, and string lengths without adding another dependency.

`IValidatableObject` is used where one field depends on another:

```text
StartDate must be before EndDate.
```

## Remove Premium Range Validation From DTOs

Decision:

```text
Do not use a Range attribute on PremiumAmount in request DTOs.
```

Reason:

We chose to keep DTO validation minimal. If positive premium validation is needed, it can be enforced later in `PolicyService` as a business rule.

## Use CancelPolicyRequest

Decision:

```text
Use a request DTO for cancelling a policy.
```

Reason:

Cancelling a policy is not a physical delete. It is a lifecycle action that should store cancellation metadata.

Current request body:

```json
{
  "reason": "Customer requested cancellation"
}
```

Later flow:

```text
POST /api/policies/{id}/cancel
Controller binds JSON to CancelPolicyRequest.
Service sets Status, CancelledAt, and CancellationReason.
```

## Use PolicyFilter for Query Parameters

Decision:

```text
Use PolicyFilter to group optional policy list filters.
```

Reason:

It keeps the future policy listing API cleaner than passing several optional parameters separately.

Example future requests:

```http
GET /api/policies?customerId={id}
GET /api/policies?productCode=CAR
GET /api/policies?status=Active
```

## Do Not Add a Repository Layer Yet

Decision:

```text
CustomerService uses AppDbContext directly.
No ICustomerRepository is added now.
```

Reason:

EF Core already provides repository-like behavior through `DbSet` and unit-of-work behavior through `DbContext`. A repository layer would mostly wrap EF calls without adding value at this size.

Current flow:

```text
Controller -> Service -> AppDbContext -> Database
```

A repository can be added later if queries become complex, shared, or if the application needs stronger isolation from EF Core.

## Use CancellationToken in Async Service Methods

Decision:

```text
Async service methods accept CancellationToken cancellationToken = default.
```

Reason:

ASP.NET Core can cancel work when a request is aborted, times out, or the application shuts down. Passing the token into EF Core lets database operations stop when they are no longer needed.

The `= default` keeps calls simple in tests and internal code:

```csharp
await customerService.GetByIdAsync(id);
```

## Use Application Exceptions for Service Outcomes

Decision:

```text
Use NotFoundException for missing resources.
Use ConflictException for business rule conflicts.
```

Reason:

Services should not return HTTP responses directly. They express the business outcome, and controllers or global error handling can later map those outcomes to HTTP responses.

Planned mapping:

```text
NotFoundException -> 404 Not Found
ConflictException -> 409 Conflict
```

## Use Branches Per Step

Decision:

```text
Each implementation step gets its own branch.
Each step is committed and pushed.
The user reviews and merges the PR before the next step starts.
```

Reason:

This keeps changes reviewable and keeps the Git history aligned with the implementation plan.
