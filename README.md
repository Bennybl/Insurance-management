# Insurance Management API

A .NET 8 Web API for managing insurance **customers** and their **policies**. It is a
layered monolith built with EF Core and PostgreSQL, with Swagger for exploring the API.

**Stack:** C# / .NET 8 · ASP.NET Core Web API · EF Core 8 · PostgreSQL (SQLite for tests) · Swagger / OpenAPI · xUnit

---

## Setup

### Run with Docker Compose (recommended)

Requires Docker Desktop / Docker Engine with the Compose plugin.

```bash
docker compose up --build
```

This starts two containers:

- `insurance-db` — PostgreSQL 16, data persisted in the `pgdata` volume.
- `insurance-api` — the API, which waits for the database to be healthy, then creates the
  schema and seeds the `PolicyProducts` on startup.

Once running:

- API base URL: <http://localhost:8080>
- Swagger UI: <http://localhost:8080/swagger>

Stop the stack (keep data):

```bash
docker compose down
```

Stop and delete the database volume (fresh start):

```bash
docker compose down -v
```

#### Configuration

Defaults work out of the box. Override via environment variables (or a `.env` file) before `docker compose up`:

| Variable            | Default      | Description                          |
| ------------------- | ------------ | ------------------------------------ |
| `POSTGRES_DB`       | `insurance`  | Database name                        |
| `POSTGRES_USER`     | `insurance`  | Database user                        |
| `POSTGRES_PASSWORD` | `insurance`  | Database password (dev only)         |
| `POSTGRES_PORT`     | `5432`       | Host port mapped to Postgres         |
| `API_PORT`          | `8080`       | Host port mapped to the API          |

The API reads its connection string from `ConnectionStrings__DefaultConnection`,
which Compose points at the `db` service.

### Run locally without Docker

Requires the .NET 8 SDK and a reachable PostgreSQL instance. The default connection string
in `appsettings.json` expects Postgres on `localhost:5432` (database/user/password all
`insurance`). The quickest way to get that is to start just the database container:

```bash
docker compose up -d db
dotnet run --project src/InsuranceManagement.Api
```

### Tests

```bash
dotnet test
```

Tests use an in-memory SQLite database and do not require Docker or PostgreSQL.

---

## Architecture

### Design

The solution is a **layered monolith**. A request flows through clear, single-responsibility layers:

```text
Controller  ->  Service  ->  Repository  ->  AppDbContext  ->  PostgreSQL
```

- **Controllers** are thin. They only handle HTTP concerns (routing, status codes, model
  binding) and delegate to services.
- **Services** (`CustomerService`, `PolicyService`) hold the business rules and map domain
  entities to/from DTOs. They express outcomes by throwing application exceptions
  (`NotFoundException`, `ConflictException`) rather than returning HTTP results.
- **Repositories** (`ICustomerRepository`, `IPolicyRepository`) own all data access and hide
  EF Core / provider details behind interfaces. They share the request-scoped `AppDbContext`,
  so it remains the **unit of work** — e.g. deactivating a customer cancels that customer's
  active policies in a single `SaveChangesAsync` (one transaction).
- A **global exception handler** translates exceptions into RFC 7807 `ProblemDetails`
  responses: `NotFoundException` → 404, `ConflictException` and DB unique-constraint
  violations → 409, malformed JSON / bad requests → 400, anything else → 500. Request DTO
  validation (`DataAnnotations` + `IValidatableObject`) returns a consistent 400 payload.
- On startup the app **creates the schema and seeds the product lookup** automatically
  (`EnsureCreated`), so the containerized stack is ready to use with no manual migration step.

Key design decisions and their rationale are recorded in [`docs/decisions.md`](docs/decisions.md).

### Data model

```text
Customer 1 ────< Policy >──── 1 PolicyProduct
```

- **Customer** — `Id` (GUID), `FirstName`, `LastName`, `Email` (**unique**), `PhoneNumber`,
  `Address`, `IsActive`, `CreatedAt`, `UpdatedAt`. Has many `Policies`.
- **Policy** — `Id` (GUID), `CustomerId` (FK → Customer), `PolicyNumber`
  (**unique**, system-generated, e.g. `POL-20260626-AB12CD34EF56`), `ProductCode`
  (FK → PolicyProduct), `Status` (`Active` / `Cancelled` / `Expired`, stored as text),
  `StartDate`, `EndDate` (calendar dates), `PremiumAmount` (`decimal(18,2)`), `DetailsJson`
  (optional free-form per-product details), and audit fields `CreatedAt`, `UpdatedAt`,
  `CancelledAt`, `CancellationReason`.
- **PolicyProduct** — `Code` (PK, e.g. `CAR`), `Name`, `IsActive`. A seeded **lookup table**
  (`CAR`, `HEALTH`, `LIFE`, `HOME`, `TRAVEL`). New products are added via the database, not an API.

Relationships use `OnDelete: Restrict` (no cascade deletes); customers are deactivated, never
hard-deleted. Indexes back the unique constraints (`Customer.Email`, `Policy.PolicyNumber`) and
the common policy filters (`CustomerId`, `ProductCode`, `Status`).

### API endpoints

| Method & route                       | Purpose                                            |
| ------------------------------------ | -------------------------------------------------- |
| `POST   /api/customers`              | Create a customer                                  |
| `GET    /api/customers`              | List customers                                     |
| `GET    /api/customers/{id}`         | Get a customer                                     |
| `PUT    /api/customers/{id}`         | Update a customer                                  |
| `DELETE /api/customers/{id}`         | Deactivate a customer (cancels active policies)    |
| `GET    /api/customers/{id}/policies`| List a customer's policies                         |
| `POST   /api/policies`               | Issue a policy                                     |
| `GET    /api/policies`               | List / filter policies (customer, product, status) |
| `GET    /api/policies/{id}`          | Get a policy                                        |
| `PUT    /api/policies/{id}`          | Update a policy                                     |
| `POST   /api/policies/{id}/cancel`   | Cancel a policy                                     |

---

## Assumptions

Business assumptions made while implementing the challenge:

- **Internal/back-office tool.** The API is used by trusted agents; authentication and
  authorization are out of scope.
- **One account per email.** Customer email is unique; a duplicate is rejected as a conflict.
- **Customers are soft-deleted.** "Delete" deactivates the customer (`IsActive = false`) and
  cancels their active policies; records are never physically removed (preserves history).
- **Customers can hold multiple policies**, across any of the available products.
- **Policy types are a controlled lookup.** Only seeded products (`CAR`, `HEALTH`, `LIFE`,
  `HOME`, `TRAVEL`) can be used; managing products is a database task, not an exposed endpoint.
  A policy can only be issued for an **active** product and to an **active** customer.
- **Policy numbers are system-generated** and unique; clients do not supply them.
- **A cancelled policy is terminal** — it cannot be updated or cancelled again.
- **Premiums are positive** and the start date must be before the end date.
- **Currency and coverage amount are not modeled.** A single implicit currency is assumed;
  these can be added later if requirements demand it.
- **`DetailsJson` is free-form.** Product-specific detail schemas are not validated server-side.
- **`Expired` status is modeled but not auto-applied.** There is no background job to expire
  policies past their end date yet; it is left as a future improvement.
- **Timestamps are UTC** (`DateTimeOffset`), and policy dates are calendar dates with no time-of-day.
