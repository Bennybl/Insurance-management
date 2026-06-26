# Insurance Management System - Implementation Plan

## Purpose

This document describes the implementation plan for the **Backend Challenge - Insurance Management System - Ai.Gent**.

The goal is to build a small, correct, and professional backend API that allows an insurance agent to manage customers and their insurance policies.

The solution should stay focused on the assignment requirements, while keeping the design easy to extend later.

---

## Core Requirements Covered

The system must support:

- Customer onboarding and management.
- Policy issuance to an existing customer.
- Retrieving customers and policies.
- Filtering policies by customer, product/type, and status.
- Updating policy details.
- Cancelling/terminating an existing policy.
- Enforcing business rules and data integrity.
- Clear project structure, README, and setup instructions.

---

## Recommended Stack

- **C# / .NET 8 Web API**
- **EF Core**
- **SQL database**: SQLite for simple local setup, or PostgreSQL if Docker Compose is used
- **Swagger/OpenAPI**
- **FluentValidation** or simple request validation
- **xUnit** for tests

The project should be a **layered monolith**, not microservices.

---

## Architecture

Recommended structure:

```text
InsuranceManagement.Api
├── Controllers
│   ├── CustomersController
│   └── PoliciesController
│
├── Application
│   ├── Customers
│   │   ├── CustomerService
│   │   ├── CreateCustomerRequest
│   │   ├── UpdateCustomerRequest
│   │   └── CustomerResponse
│   │
│   └── Policies
│       ├── PolicyService
│       ├── IssuePolicyRequest
│       ├── UpdatePolicyRequest
│       ├── CancelPolicyRequest
│       ├── PolicyFilter
│       └── PolicyResponse
│
├── Domain
│   ├── Customer
│   ├── Policy
│   ├── PolicyProduct
│   ├── PolicyStatus
│   └── BusinessRules
│
├── Infrastructure
│   ├── AppDbContext
│   ├── Configurations
│   ├── SeedData
│   └── Migrations
│
└── Tests
    ├── CustomerServiceTests
    └── PolicyServiceTests
```

Main rule:

```text
Controllers handle HTTP.
Services handle business logic.
EF Core handles persistence.
DTOs are used for API input/output.
Entities are not exposed directly from the API.
```

---

## Data Model

### Customer

```text
Customer
- Id
- FirstName
- LastName
- Email
- PhoneNumber
- NationalId
- DateOfBirth
- Address
- IsActive
- CreatedAt
- UpdatedAt
```

Purpose:

- Represents the policyholder.
- Can own many policies.
- Uses soft delete/deactivation instead of physical deletion.

Important constraints:

```text
Email is unique.
NationalId is unique if provided.
Id is the primary key.
```

---

### PolicyProduct

```text
PolicyProduct
- Code
- Name
- IsActive
```

Example seed values:

```text
CAR      Car Insurance      true
HEALTH   Health Insurance   true
LIFE     Life Insurance     true
HOME     Home Insurance     true
TRAVEL   Travel Insurance   true
```

Purpose:

- Defines valid insurance products.
- Prevents invalid product codes.
- Keeps the solution easy to extend.
- Allows adding simple future products without changing the policy table schema.

Important scope decision:

```text
PolicyProducts is a seeded lookup table only.
There is no PolicyProductsController.
There is no product-management API.
```

This keeps the project focused on the assignment requirements.

---

### Policy

```text
Policy
- Id
- CustomerId
- PolicyNumber
- ProductCode
- Status
- StartDate
- EndDate
- PremiumAmount
- CoverageAmount
- Currency
- DetailsJson
- CreatedAt
- UpdatedAt
- CancelledAt
- CancellationReason
```

Purpose:

- Represents an issued insurance policy.
- Belongs to one customer.
- References one policy product.
- Has a lifecycle status.

Relationships:

```text
Customer 1 ---- many Policies
PolicyProduct 1 ---- many Policies
```

Important constraints:

```text
Policy.CustomerId references Customer.Id.
Policy.ProductCode references PolicyProduct.Code.
Policy.PolicyNumber is unique.
PremiumAmount must be positive.
CoverageAmount must be positive.
StartDate must be before EndDate.
```

---

## SQL vs NoSQL Decision

The recommended choice is **SQL**.

Reason:

```text
The core domain has strong relationships and consistency requirements:
- A policy must belong to an existing customer.
- A policy must use a valid product.
- Email and policy number must be unique.
- Customers with active policies cannot be deleted/deactivated.
- Policies need filtering by customer, product, status, and date.
```

SQL gives:

```text
Foreign keys
Unique constraints
Indexes
Transactions
Clear relationships
Reliable filtering
```

NoSQL is not necessary for this scope because the main problem is relational and consistency-focused.

---

## Why DetailsJson Exists

Not all policy products may have the exact same fields in the future.

Example:

```text
Car policy may need license plate and vehicle model.
Life policy may need beneficiaries.
Health policy may need coverage tier.
```

The common fields stay as normal SQL columns:

```text
CustomerId
ProductCode
Status
StartDate
EndDate
PremiumAmount
CoverageAmount
Currency
```

The flexible, product-specific fields can go into:

```text
DetailsJson
```

Example car details:

```json
{
  "licensePlate": "123-45-678",
  "vehicleMake": "Toyota",
  "vehicleModel": "Corolla",
  "vehicleYear": 2021
}
```

Important scope decision:

```text
DetailsJson is only an extension point.
The core assignment should not become a complex polymorphic policy engine.
```

---

## API Endpoints

### Customers

```http
POST   /api/customers
GET    /api/customers
GET    /api/customers/{id}
PUT    /api/customers/{id}
DELETE /api/customers/{id}
GET    /api/customers/{id}/policies
```

Delete should be implemented as soft delete/deactivation:

```text
IsActive = false
```

---

### Policies

```http
POST   /api/policies
GET    /api/policies
GET    /api/policies/{id}
PUT    /api/policies/{id}
POST   /api/policies/{id}/cancel
```

Filtering:

```http
GET /api/policies?customerId={customerId}
GET /api/policies?productCode=CAR
GET /api/policies?status=Active
GET /api/policies?productCode=CAR&status=Active
```

---

## Business Rules

### Customer Rules

```text
Cannot create two customers with the same email.
Cannot create two customers with the same national ID if provided.
Cannot issue a policy to an inactive customer.
Cannot deactivate a customer with active policies.
Customers are not physically deleted.
```

---

### Policy Rules

```text
Policy must belong to an existing customer.
Policy must use an existing PolicyProduct.
Cannot issue a policy for an inactive PolicyProduct.
StartDate must be before EndDate.
PremiumAmount must be positive.
CoverageAmount must be positive.
PolicyNumber must be unique.
Cannot update a cancelled policy.
Cannot cancel an already cancelled policy.
Cancelled policies keep historical data.
```

Optional, if time allows:

```text
A customer cannot have two active policies of the same product with overlapping dates.
```

---

## Error Handling

The API should return clear HTTP errors:

```text
400 Bad Request - invalid input or validation failure
404 Not Found - customer/policy not found
409 Conflict - duplicate email, duplicate national ID, active policies prevent delete, invalid lifecycle action
500 Internal Server Error - unexpected errors only
```

Avoid leaking raw exceptions to the client.

Recommended response style:

```json
{
  "error": "Policy cannot be cancelled because it is already cancelled."
}
```

Or use ASP.NET Core `ProblemDetails`.

---

## Testing Strategy

Do not aim for 100% coverage. Focus on important business logic.

Recommended tests:

```text
Should create customer with valid data.
Should reject duplicate customer email.
Should issue policy for active customer.
Should reject policy for missing customer.
Should reject policy for inactive customer.
Should reject invalid policy dates.
Should reject unknown product code.
Should cancel active policy.
Should reject cancelling already cancelled policy.
Should reject updating cancelled policy.
Should filter policies by product/status/customer.
```

Prioritize service-level tests first.

---

# Step-by-Step Implementation Plan

Each step should be reviewed and approved before moving to the next one.

---

## Step 1 - Create Project Skeleton

Tasks:

```text
Create .NET 8 Web API project.
Add basic folder structure: Controllers, Application, Domain, Infrastructure, Tests.
Add Swagger.
Add EF Core packages.
Add initial Program.cs wiring.
```

Expected result:

```text
Application runs locally.
Swagger opens successfully.
No business logic yet.
```

Review checkpoint:

```text
Approve project structure and local run setup before continuing.
```

---

## Step 2 - Create Domain Entities

Tasks:

```text
Create Customer entity.
Create Policy entity.
Create PolicyProduct entity.
Create PolicyStatus enum.
Define relationships in code.
```

Expected result:

```text
The domain model clearly shows:
Customer has many Policies.
Policy belongs to Customer.
Policy references PolicyProduct.
```

Review checkpoint:

```text
Approve entity fields, names, and relationships before continuing.
```

---

## Step 3 - Configure Database and EF Core

Tasks:

```text
Create AppDbContext.
Configure Customer table.
Configure Policy table.
Configure PolicyProduct table.
Add foreign keys.
Add unique constraints.
Add indexes.
Seed initial PolicyProducts.
Create initial migration.
```

Important database rules:

```text
Customer.Email unique.
Customer.NationalId unique when provided.
Policy.PolicyNumber unique.
Policy.CustomerId foreign key.
Policy.ProductCode foreign key.
Indexes on CustomerId, ProductCode, Status.
```

Expected result:

```text
Database schema exists.
Initial products are seeded: CAR, HEALTH, LIFE, HOME, TRAVEL.
```

Review checkpoint:

```text
Approve database schema and seed data before continuing.
```

---

## Step 4 - Add DTOs and Request Validation

Tasks:

```text
Create CreateCustomerRequest.
Create UpdateCustomerRequest.
Create CustomerResponse.
Create IssuePolicyRequest.
Create UpdatePolicyRequest.
Create CancelPolicyRequest.
Create PolicyResponse.
Create PolicyFilter.
Add validation for required fields, email, dates, and positive amounts.
```

Expected result:

```text
API input/output models are separated from EF entities.
Invalid requests are rejected clearly.
```

Review checkpoint:

```text
Approve request/response contracts before implementing services.
```

---

## Step 5 - Implement CustomerService

Tasks:

```text
Create customer.
Get customer by id.
List customers.
Update customer.
Deactivate customer instead of physical delete.
Get policies for customer.
Enforce customer business rules.
```

Business rules:

```text
Email must be unique.
NationalId must be unique if provided.
Cannot deactivate customer with active policies.
```

Expected result:

```text
Customer behavior works through the service layer.
Controller should stay thin.
```

Review checkpoint:

```text
Approve customer service behavior before implementing policy logic.
```

---

## Step 6 - Implement CustomersController

Tasks:

```text
Expose customer endpoints.
Map HTTP requests to CustomerService methods.
Return clean responses.
Return proper status codes.
```

Endpoints:

```http
POST   /api/customers
GET    /api/customers
GET    /api/customers/{id}
PUT    /api/customers/{id}
DELETE /api/customers/{id}
GET    /api/customers/{id}/policies
```

Expected result:

```text
Customer flow can be tested from Swagger/Postman.
```

Review checkpoint:

```text
Approve customer API behavior before moving to policies.
```

---

## Step 7 - Implement PolicyService

Tasks:

```text
Issue policy.
Get policy by id.
List/filter policies.
Update policy.
Cancel policy.
Generate policy number.
Enforce policy business rules.
```

Business rules:

```text
Customer must exist.
Customer must be active.
PolicyProduct must exist.
PolicyProduct must be active.
StartDate must be before EndDate.
PremiumAmount must be positive.
CoverageAmount must be positive.
Cannot update cancelled policy.
Cannot cancel already cancelled policy.
```

Optional rule:

```text
Prevent overlapping active policies of the same product for the same customer.
```

Expected result:

```text
Policy lifecycle works through the service layer.
```

Review checkpoint:

```text
Approve policy service behavior before exposing policy endpoints.
```

---

## Step 8 - Implement PoliciesController

Tasks:

```text
Expose policy endpoints.
Map HTTP requests to PolicyService methods.
Support filters by customerId, productCode, and status.
Return proper status codes.
```

Endpoints:

```http
POST   /api/policies
GET    /api/policies
GET    /api/policies/{id}
PUT    /api/policies/{id}
POST   /api/policies/{id}/cancel
```

Expected result:

```text
Policy flow can be tested from Swagger/Postman.
```

Review checkpoint:

```text
Approve policy API behavior before adding polish and tests.
```

---

## Step 9 - Add Global Error Handling

Tasks:

```text
Create custom exceptions or result objects.
Map validation errors to 400.
Map not found errors to 404.
Map business conflicts to 409.
Ensure unexpected errors return 500.
```

Expected result:

```text
The API returns consistent, clean errors.
No raw stack traces are returned to the client.
```

Review checkpoint:

```text
Approve error response format and status-code mapping.
```

---

## Step 10 - Add Tests

Tasks:

```text
Add service-level tests for main business rules.
Add a few API smoke tests if time allows.
```

Priority tests:

```text
Create customer successfully.
Reject duplicate email.
Issue policy successfully.
Reject policy for missing customer.
Reject policy for inactive customer.
Reject invalid policy dates.
Reject unknown product code.
Cancel active policy.
Reject cancelling already cancelled policy.
Reject updating cancelled policy.
Filter policies by product/status/customer.
```

Expected result:

```text
Important business logic is protected by tests.
```

Review checkpoint:

```text
Approve test coverage before final cleanup.
```

---

## Step 11 - Manual QA Through Swagger/Postman

Tasks:

```text
Create sample customer.
Issue sample policies.
Filter policies.
Update policy.
Cancel policy.
Try invalid requests.
Check error responses.
```

Manual scenarios:

```text
Create customer -> issue policy -> get customer policies.
Try issuing policy with missing customer.
Try issuing policy with invalid product code.
Try updating cancelled policy.
Try deleting customer with active policy.
```

Expected result:

```text
The complete user flow works end-to-end.
```

Review checkpoint:

```text
Approve manual QA results before final README polishing.
```

---

## Step 12 - Final README and Submission Cleanup

Tasks:

```text
Add setup instructions.
Add run instructions.
Add test instructions.
List main endpoints.
Explain architecture.
Explain SQL decision.
Explain PolicyProducts lookup table.
Explain DetailsJson tradeoff.
Document assumptions.
Document future improvements.
Clean unused code.
Check formatting and naming.
```

Expected result:

```text
Repository is ready for review.
Reviewer can understand and run the project easily.
```

Review checkpoint:

```text
Approve final submission before sending GitHub link.
```

---

# Assumptions

```text
A customer can have multiple policies.
Policies are not physically deleted.
Cancelling a policy changes its status and stores cancellation metadata.
Customers are deactivated instead of deleted.
PolicyProducts are seeded/configured, not managed by the public API.
DetailsJson is optional and used only for product-specific extension data.
```

---

# Deliberately Not Included

To keep the project aligned with the 5-hour scope, the following are intentionally excluded:

```text
Authentication and authorization
Microservices
CQRS
Event sourcing
Kafka/RabbitMQ
Complex product-management module
Pricing engine
Dynamic rules engine
Advanced audit logging
Background jobs
```

These can be mentioned as possible future improvements, but should not be implemented unless time remains and the core scope is complete.

---

# Future Improvements

Possible future extensions:

```text
Authentication and role-based access for agents.
Audit log for policy changes.
Background job to expire policies after EndDate.
Product-specific validators for DetailsJson.
Dedicated details tables for mature product-specific models.
Pagination for all list endpoints.
More integration tests.
Docker Compose with PostgreSQL.
```

---

# Final Design Summary

The solution is intentionally simple and requirement-focused:

```text
Customers
Policies
Policy lifecycle
Filtering
Validation
Data integrity
Clear README
```

It is also easy to extend:

```text
PolicyProducts defines valid products.
DetailsJson allows product-specific fields.
Service-layer validators allow future product-specific rules.
The core customer-policy relationship stays stable.
```

This provides a good balance for a SWE home assignment:

```text
Small enough to finish.
Correct enough to trust.
Clean enough to review.
Flexible enough to extend.
```
