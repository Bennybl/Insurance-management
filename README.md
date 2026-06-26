# Insurance Management API

A .NET 8 Web API for managing customers and insurance policies (layered monolith, EF Core, PostgreSQL).

## Run with Docker Compose (recommended)

Requires Docker Desktop / Docker Engine with the Compose plugin.

```bash
docker compose up --build
```

This starts two containers:

- `insurance-db` — PostgreSQL 16, data persisted in the `pgdata` volume.
- `insurance-api` — the API, which waits for the database to be healthy, creates the
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

### Configuration

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

## Run locally without Docker

Requires the .NET 8 SDK and a reachable PostgreSQL instance. The default
connection string in `appsettings.json` expects Postgres on
`localhost:5432` (database/user/password all `insurance`). The quickest way to
get that is to start just the database container:

```bash
docker compose up -d db
dotnet run --project src/InsuranceManagement.Api
```

## Tests

```bash
dotnet test
```

Tests use an in-memory SQLite database and do not require Docker or PostgreSQL.
