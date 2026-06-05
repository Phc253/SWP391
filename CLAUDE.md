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

**JWT claims:** `ClaimTypes.NameIdentifier` → UserId, `ClaimTypes.Email` → Email, `ClaimTypes.Role` → role(s). Token lifetime is 7 days (hardcoded in `AccountService.LoginAsync`; `Jwt:ExpireMinutes` in config is not currently used).

**Password hashing:** PBKDF2 + SHA-256, 10,000 iterations, 16-byte salt. Stored as `{base64(salt)}.{base64(hash)}` in `User.PasswordHash`.

**Pagination contract:** Repositories return `(List<T> items, int totalCount)`. Controllers accept `page` (default 1) and `pageSize` (default 10–20, clamped 1–100).

**AutoMapper:** Used in services to map entities → DTOs. Profiles are registered in `Program.cs`.

## Business Domains

**OpenAlex sync:** `AcademicDataIntegrationService` fetches papers from OpenAlex and upserts by `ExternalId`. OpenAlex concepts are normalized by level — Level 0–1 become `ResearchTopic`, Level 2+ become `Keyword`. Sync history is recorded in `SyncJob`. Triggered via `POST /api/datasync/sync-openalex` (AdminOnly).

**Trend analysis:** `TrendService` queries paper counts by year per keyword or topic. `PublicationTrend` is a materialized cache table populated by `POST /api/trends/compute-trends` (AdminOnly) — run this after a sync to keep trend data fresh. Activity Score combines recent paper count + YoY growth rate, normalized to 0–100.

**Email verification:** Registration creates an `EmailVerificationToken` record. The token is emailed via SMTP (configured under `Email:` in `appsettings.json`). `GET /api/account/verify-email?token=` validates and activates the account. Unverified accounts cannot log in.

**User personalization:** Bookmarks and follows use a generic `TargetType` string (`"Paper"`, `"Keyword"`, `"Author"`) with `TargetId`, toggling on repeated calls.

## Configuration

`appsettings.json` holds:
- `ConnectionStrings:DefaultConnection` — SQL Server (local instance `MINHDUCK\DUCDEPTRAI`, Windows auth)
- `Jwt:Key`, `Jwt:Issuer`, `Jwt:Audience`
- `Email:SmtpHost`, `Email:SmtpPort`, `Email:SenderEmail`, `Email:SenderPassword`, `Email:VerificationUrl`

CORS is currently open to all origins — development configuration.

## Known Issues (see `missingOfRegister.md`)

- **Privilege escalation:** `RoleId` is accepted from the client on registration; it should be hardcoded to the Member role.
- **No token revocation:** Logout is client-side only; the server-issued JWT remains valid until expiry.
- **No refresh token mechanism.**
