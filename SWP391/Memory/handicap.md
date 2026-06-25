# Project Handicap / Gap Report

Snapshot date: 2026-06-19  
Repository: `SWP391` - ASP.NET Core Web API for Scientific Journal Publication Trend Tracking System  
Scope of this note: summarize current completion level, known gaps, production risks, and next recommended actions. No code changes were made during this review.

## 1. Executive Summary

The project is already strong enough for workflow demo and class presentation, especially because the main API surfaces exist for authentication, paper discovery, sync, trends, dashboard, reports, bookmark, follow, notification, and admin.

However, the project is not production-ready yet. The main handicaps are:

- Error handling still exposes raw exception messages in many service responses.
- `appsettings.json` contains hard-coded secrets and environment-specific config.
- Register/login has improved, but still misses refresh token, forgot/reset password, generic login failure, and rate limiting.
- Data sync works as a batch OpenAlex pipeline, but it is not a true incremental sync yet.
- Duplicate protection is mostly app-level; several important database unique indexes are missing.
- There is no automated test project.
- `.github/workflows` exists but currently has no workflow file, so CI/CD is 0%.

Build status from review:

```text
dotnet build SWP391.sln
Result: succeeded
Warnings: 25 nullability warnings
Errors: 0
```

## 2. Completion Estimate

These percentages are estimates based on current API coverage, service logic, and production readiness.

| Area | Demo/API Coverage | Production Readiness | Notes |
|---|---:|---:|---|
| Workflow 1 - Data Acquisition & Synchronization | ~80% | ~60% | Manual sync, scheduler, SyncJob, notification, trend recompute exist. Missing true incremental sync/checkpoint. |
| Workflow 2 - Register/Login/Search/Bookmark/Follow | ~85% | ~65% | Main flow exists. Security hardening and auth lifecycle still incomplete. |
| Workflow 3 - Trend Analysis & Intelligence | ~80% | ~70% | Trend endpoints and compute stages exist. Needs validation, testing, performance review. |
| Workflow 4 - Dashboard/Report/Admin | ~78% | ~65% | Dashboard/report/admin APIs exist. Needs cleaner error contract, route normalization, tests. |
| Functional Requirements FR-01 to FR-30 overall | ~72% | ~58-60% | Good demo coverage, moderate production risk. |
| GitHub Actions workflows | 0% | 0% | `.github/workflows` folder is empty. |

## 3. Workflow Review

### 3.1 Workflow 1 - Data Acquisition & Synchronization

Current implemented flow:

```text
Admin or scheduler trigger
-> DataSyncService.SyncOpenAlexAsync(keyword, maxResults)
-> AcademicDataIntegrationService.FetchAndSaveDataFromOpenAlexAsync(keyword, maxResults)
-> call OpenAlex /works?search={keyword}&per-page={maxResults}
-> normalize papers/journals/authors/topics/keywords
-> insert new papers or update citation count for existing papers
-> trigger notifications for matched follows
-> recompute trends
-> update SyncJobs status
```

Important files:

- `SWP391/Controllers/DataSyncController.cs`
- `SWP391/Service/DataSyncService.cs`
- `SWP391/Service/AcademicDataIntegrationService.cs`
- `SWP391/Service/DataSyncSchedulerHostedService.cs`
- `SWP391/Service/NotificationTriggerService.cs`
- `SWP391/Memory/ConfirmFetchnSync.md`
- `SWP391/Memory/Flow1Status.md`

What is already good:

- Manual admin API exists: `POST /api/DataSync/sync-openalex`.
- Scheduler hosted service exists.
- Admin can view/update/enable/disable scheduler config.
- SyncJobs record status: `Running`, `Completed`, `CompletedWithWarnings`, `Failed`.
- New paper IDs are passed to notification trigger.
- Notifications are created for users following matching journal/topic.
- Trend computation runs after sync.

Main handicaps:

- Current sync is batch search, not true incremental sync.
- No `SyncCheckpoint` or last fetched publication/update time.
- No cursor paging for OpenAlex.
- `maxResults` is clamped to 200, while OpenAlex list endpoint is safer around `per-page <= 100`.
- Search ranking may return old high-citation papers instead of newest papers.
- Fetch new works and refresh existing works are mixed in one pipeline.
- Database unique index for `(SourceId, ExternalId)` is missing.
- Notification duplicate protection is app-level only, not database-level.

Production direction:

```text
Job 1: Fetch New Works
-> use keyword/topic config
-> read checkpoint
-> call OpenAlex with publication/update date filter
-> sort by publication_date desc or updated_date desc
-> use cursor paging
-> insert only new works
-> update checkpoint
-> notify only for newly inserted papers

Job 2: Refresh Existing Works
-> choose existing papers older than refresh threshold
-> call OpenAlex by external ID/DOI
-> update citation count / metadata
-> do not send "new paper" notifications
```

### 3.2 Workflow 2 - Research Discovery & User Interaction

Expected workflow:

```text
register
-> verify email
-> login
-> search papers
-> filter by keyword / author / journal
-> view paper detail
-> bookmark papers/keywords
-> follow journals/topics/authors
-> view bookmarks/follows
```

Important files:

- `SWP391/Controllers/AccountController.cs`
- `SWP391/Service/AccountService.cs`
- `SWP391/Controllers/PapersController.cs`
- `SWP391/Service/PaperService.cs`
- `SWP391/Repositories/PaperRepository.cs`
- `SWP391/Controllers/BookmarksController.cs`
- `SWP391/Service/BookmarkService.cs`
- `SWP391/Controllers/FollowsController.cs`
- `SWP391/Service/FollowService.cs`
- `SWP391/missingOfRegister.md`

What is already good:

- Register, login, logout, email verify exist.
- Public register no longer accepts `RoleId` directly.
- Password hashing uses PBKDF2 + salt.
- JWT auth and role policies exist.
- Revoked token table and logout revocation exist.
- Search paper API supports keyword, author, journal, pagination.
- Paper detail API exists: `GET /api/Papers/{id}`.
- Bookmark paper/keyword exists.
- Follow author/journal/research topic exists.
- My bookmarks and my follows APIs exist.

Decision on search/filter API:

Keeping keyword/author/journal in one `GET /api/Papers` API is professional and frontend-friendly. It allows combined filters, simple pagination, and fewer redundant endpoints. Separate APIs are only needed if each search mode has very different ranking, response, or performance behavior.

Main handicaps:

- Login returns distinguishable errors such as user not found vs invalid password.
- Access token lasts 7 days; no refresh token.
- Forgot password / reset password is missing.
- Change password / update profile is missing.
- Login/register rate limiting and account lockout are missing.
- SMTP failure during register can break the request after user/token creation.
- Public register allows client-chosen `ActorType`; not immediately privilege escalation, but should not be used as authorization.
- Duplicate bookmark/follow protection has no database unique index.

### 3.3 Workflow 3 - Trend Analysis & Intelligence

Important files:

- `SWP391/Controllers/TrendsController.cs`
- `SWP391/Service/TrendService.cs`
- `SWP391/Repositories/TrendRepository.cs`
- `SWP391/Service/Trends/TrendScoring.cs`

Implemented endpoints include:

- `GET /api/Trends/keyword`
- `GET /api/Trends/trending`
- `GET /api/Trends/topic`
- `GET /api/Trends/growth`
- `GET /api/Trends/topic-growth`
- `GET /api/Trends/activity-score`
- `POST /api/Trends/compute-trends`
- `POST /api/Trends/compute/keywords`
- `POST /api/Trends/compute/topics`
- `POST /api/Trends/compute/snapshots`
- `GET /api/Trends/snapshot-history`
- `GET /api/Trends/topic-snapshot-history`

What is already good:

- Trend compute is split into demo-friendly stages.
- Keyword and topic trends are supported.
- Snapshot-based activity score exists.
- Trend compute is called after sync.

Main handicaps:

- Many service errors still append `ex.Message`.
- Performance is not proven on large datasets.
- No automated tests for scoring and trend aggregation.
- Some nullable warnings exist in trend repository and models.

### 3.4 Workflow 4 - Visualization, Reporting & Administration

Important files:

- `SWP391/Controllers/DashboardController.cs`
- `SWP391/Controllers/DashboardReportsController.cs`
- `SWP391/Service/DashboardService.cs`
- `SWP391/Repositories/DashboardRepository.cs`
- `SWP391/Controllers/ReportController.cs`
- `SWP391/Service/ReportService.cs`
- `SWP391/Controllers/AdminController.cs`
- `SWP391/Service/AdminService.cs`

What is already good:

- System dashboard summary exists.
- Personalized dashboard exists.
- Saved dashboard report CRUD exists.
- Paper report and keyword stats exist.
- CSV and PDF export exist.
- Admin user management exists.
- Admin role assignment/removal exists.
- Admin sync jobs/settings/activity logs/stats exist.
- Scheduler config API exists.

Main handicaps:

- Error responses are inconsistent across controllers.
- Some endpoints use `api/[controller]`, which results in capitalized route segments like `/api/Papers`.
- No tests for ownership protection on saved reports/notifications.
- Report export with very large datasets may be heavy.

## 4. API Error Handling Handicap

Current issue:

Many services catch exceptions and return `ServiceResult.Fail("... " + ex.Message)`. Controllers then return that message to API consumers. This leaks implementation details and creates inconsistent error contracts.

Examples:

- `SWP391/Middlewares/ExceptionMiddleware.cs`
- `SWP391/Service/AdminService.cs`
- `SWP391/Service/DashboardService.cs`
- `SWP391/Service/DataSyncService.cs`
- `SWP391/Service/NotificationService.cs`
- `SWP391/Service/ReportService.cs`
- `SWP391/Service/TrendService.cs`

Recommended API error contract:

```json
{
  "success": false,
  "code": "RESOURCE_NOT_FOUND",
  "message": "Paper not found.",
  "traceId": "00-..."
}
```

Recommended policy:

- Validation errors: return `400` with safe message.
- Auth errors: return `401` or `403` with generic message.
- Not found: return `404`.
- Conflict/duplicate: return `409`.
- Unexpected exceptions: log internally, return generic `500`.
- Do not expose stack trace or raw exception message to client.
- Keep detailed exception message only in logs.

Important note:

Expected non-happy-case business errors should not be thrown as raw exceptions. They should be returned as controlled failures with known error codes.

## 5. Login/Register Production Handicap

Current good points:

- PBKDF2 password hashing with salt exists.
- JWT authentication exists.
- Role-based policies exist.
- Email verification exists.
- User is inactive until email verification.
- Logout revokes token by storing token hash.
- Public register no longer accepts `RoleId`.

Main remaining risks:

### 5.1 Secrets in repository

`SWP391/appsettings.json` contains:

- Database connection string.
- JWT secret.
- Gmail SMTP username/password.

This must be fixed before production:

- Rotate the exposed Gmail app password.
- Rotate JWT secret.
- Move secrets to environment variables, user-secrets, or deployment secret manager.
- Keep only non-sensitive defaults in `appsettings.json`.

### 5.2 Login enumeration

Current login can reveal whether an email exists or whether password is wrong. Production should return a generic message:

```text
Invalid email or password.
```

Internally, logs can record more detail if needed.

### 5.3 Missing refresh token

Current access token expires after 7 days. Better production model:

```text
Access token: 15-30 minutes
Refresh token: stored hashed in DB, rotated on use
Logout: revoke refresh token/session
```

### 5.4 Missing account lifecycle APIs

Recommended missing APIs:

- `POST /api/account/forgot-password`
- `POST /api/account/reset-password`
- `POST /api/account/change-password`
- `GET /api/account/profile`
- `PUT /api/account/profile`
- `POST /api/account/resend-verification-email`

### 5.5 Missing abuse protection

Recommended:

- Rate limit login/register/forgot password.
- Add failed login counter or temporary lockout.
- Avoid sending unlimited verification/forgot-password emails.

## 6. Data Integrity Handicap

Missing or recommended database unique indexes:

```text
Papers(SourceId, ExternalId)
Bookmarks(UserId, TargetType, TargetId)
Follows(UserId, TargetType, TargetId)
Notifications(UserId, RelatedType, RelatedId) filtered for RelatedType = 'Paper'
```

Reason:

App-level duplicate checks are useful, but they do not fully protect against race conditions or concurrent requests.

## 7. Route and API Design Notes

Current issue:

Many controllers use:

```csharp
[Route("api/[controller]")]
```

This creates routes like:

```text
/api/Papers
/api/Account
/api/DataSync
```

Recommended production style:

```text
/api/papers
/api/account
/api/data-sync
/api/trends
/api/admin
```

GET with query parameters is normal for search/filter:

```text
GET /api/papers?keyword=ai&author=smith&journal=nature&page=1&pageSize=10
```

Do not switch all GET filters to POST only because query parameters are visible. Query strings are appropriate for non-sensitive search filters. Sensitive data should not be sent through GET.

## 8. Git Handicap: Vim Appears When Pulling Branches

Current observed git state during review:

```text
Current branch: dev
dev is ahead of origin/dev by 1 commit
DoxDuck local is ahead of origin/DoxDuck by 4 commits
local master is behind origin/master by 37 commits
pull.rebase = false
core.editor is not configured
```

Why Vim appears:

When standing on a personal branch such as `DoxDuck` and running a pull from another branch, Git may create a merge commit. Because `core.editor` is not configured, Git opens the default editor, often Vim, to edit the merge commit message.

Examples that can trigger this:

```bash
git pull origin dev
git pull origin master
```

Safer commands:

```bash
git fetch origin
git merge --no-edit origin/dev
```

or:

```bash
git pull --no-edit origin dev
```

Set a friendlier editor:

```bash
git config --global core.editor "code --wait"
```

Recommended branch habit:

```bash
git switch dev
git pull origin dev

git switch master
git pull origin master

git switch DoxDuck
git merge --no-edit origin/dev
```

Do not use `git reset --hard` unless intentionally discarding local work.

## 9. Priority Backlog

### P0 - Must fix before production

- Remove secrets from `appsettings.json` and rotate exposed secrets.
- Stop returning raw `ex.Message` to API clients.
- Add consistent API error response contract.
- Add refresh token or reduce JWT lifetime significantly.
- Generic login failure message.
- Add rate limiting for login/register/forgot-password.
- Add database unique indexes for paper external IDs, bookmark, follow, notifications.

### P1 - Important for production quality

- Add forgot/reset password.
- Add change password and profile update.
- Add resend verification email.
- Add SyncCheckpoint table and incremental OpenAlex sync.
- Split Fetch New Works and Refresh Existing Works.
- Normalize API routes to lowercase.
- Fix nullability warnings.
- Add test project.

### P2 - Good improvements

- Add GitHub Actions CI build workflow.
- Add API response DTOs instead of anonymous objects.
- Add pagination metadata consistently.
- Add richer SyncJob metrics.
- Add cleanup job for expired revoked tokens and used/expired verification tokens.

## 10. AI Continuation Context

If another AI model continues this project, use this order:

1. Read `SWP391/Memory/ProjectOverview.md`.
2. Read `SWP391/Memory/FR.md`.
3. Read `SWP391/Memory/Workflow.md`.
4. Read `SWP391/Memory/ConfirmFetchnSync.md`.
5. Read this file: `SWP391/Memory/handicap.md`.
6. Inspect actual code before editing.

Recommended next implementation target:

```text
Create a consistent API error model and refactor service/controller error handling so raw exception messages are logged internally but not returned to clients.
```

Suggested implementation shape:

```text
Models/Common/ApiErrorResponse.cs
Models/Common/ApiResponse.cs
Models/Common/ErrorCodes.cs
Middlewares/ExceptionMiddleware.cs
ServiceResult<T> extended with ErrorCode / StatusCode
Controllers map ServiceResult failures consistently
```

Important constraint:

Do not start by rewriting all workflows at once. Fix shared API error handling first because it touches nearly every controller and will reduce production risk across all modules.
