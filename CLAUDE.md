# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

ASP.NET Core 8.0 REST API for scientific publication tracking and trend analysis. Integrates with the OpenAlex open academic database to sync paper data and expose search/trend endpoints.

## Commands

```bash
# Build
dotnet build

# Run (API on https://localhost:7174, Swagger at /swagger)
dotnet run --project SWP391/SWP391.csproj

# Run a single test
dotnet test --filter "FullyQualifiedName~TestName"

# EF Core migrations
dotnet ef migrations add <MigrationName> --project SWP391
dotnet ef database update --project SWP391
```

## Architecture

Layered architecture: **Controllers → Services → Repositories → EF Core → SQL Server**

```
SWP391/
├── Controllers/     # HTTP layer — thin, delegates to services
├── Service/         # Business logic
├── Repositories/    # Data access (EF Core queries)
├── Entities/        # EF Core entities + ScientificTrendDbContext
├── Models/          # DTOs and request/response models
└── Middlewares/     # ExceptionMiddleware (global error handling)
```

**Entry point:** `Program.cs` — configures DI, JWT, CORS, EF Core, and authorization policies.

**DbContext:** `Entities/ScientificTrendDbContext.cs` — defines all tables. The schema was scaffolded from an existing database; entities under `Entities/` are EF-generated.

## Key Design Patterns

**ServiceResult wrapper:** Services return `ServiceResult<T>` (in `Models/ServiceResult.cs`) to propagate success/failure and HTTP status codes to controllers without throwing exceptions.

**Authorization policies** (defined in `Program.cs`):
- `AdminOnly` — requires Administrator role
- `CanPublishArticle` — requires Researcher or Administrator
- `IsMember` — requires Member, Researcher, or Administrator

**JWT Auth:** `AccountService` generates tokens with roles as claims. Token expiry is configured in `appsettings.json` under `Jwt:ExpireMinutes`. Passwords use PBKDF2 + salt stored in the `User` entity.

**OpenAlex integration:** `AcademicDataIntegrationService` fetches papers from the OpenAlex API and upserts them by `ExternalId` to avoid duplicates. Triggered via `DataSyncController`.

## Configuration

`appsettings.json` holds:
- `ConnectionStrings:DefaultConnection` — SQL Server (local instance `MINHDUCK\DUCDEPTRAI`, Windows auth)
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`, `Jwt:ExpireMinutes`

CORS is currently open to all origins — development configuration.

## Known Issues (see `missingOfRegister.md`)

- **Privilege escalation:** `RoleId` is accepted from the client on registration; it should be hardcoded to the Member role.
- **No token revocation:** Logout is client-side only; the server-issued JWT remains valid until expiry.
- **No refresh token mechanism.**
