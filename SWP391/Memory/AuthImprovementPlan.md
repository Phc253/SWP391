# Auth Improvement Plan

Snapshot date: 2026-06-21

Scope: harden authentication, API error handling, rate limiting, and secret management after the class demo. This file is the long-lived context anchor before implementation. Do not treat it as a completed changelog.

## 1. Current State From Code Review

### Already Implemented

- Public register exists at `POST /api/Account/register`.
- Login exists at `POST /api/Account/login`.
- Logout exists at `POST /api/Account/logout`.
- Email verification exists at `GET /api/Account/verify-email?token=...`.
- New public registered users are created with `IsActive = false` until email verification.
- Password hashing uses PBKDF2 with per-password salt.
- JWT authentication is configured.
- Role-based policies exist: `AdminOnly`, `CanPublishArticle`, `IsMember`.
- Public `RegisterRequest` no longer contains `RoleId`.
- Public register currently assigns default role through `AccountRepository.CreateUserAsync(user)`.
- Access token revocation exists through `RevokedTokens`.
- Revoked access tokens are checked in `JwtBearerEvents.OnTokenValidated`.

### Important Remaining Gaps

- `AuthImprovementPlan.md` was empty before this update; this file now becomes the roadmap.
- `missingOfRegister.md` is outdated: it still says public register exposes `RoleId`, lacks email verification, and lacks logout/revoke.
- Current public register still accepts password on the first page. This no longer matches the teacher-reviewed flow.
- Current email verification activates an existing inactive user immediately. The new flow should verify email first, then let the user set password, then create/activate the account.
- Login returns distinguishable errors: `User not found.`, `Invalid password.`, and `Please verify your email before logging in.`
- Access token currently lasts 7 days.
- No refresh token exists.
- No forgot/reset password exists.
- No resend verification email endpoint exists.
- No change password endpoint exists.
- Profile endpoint currently returns claims only and does not support updating profile.
- No login/register/forgot-password rate limiting exists.
- `ServiceResult<T>` only stores `Success`, `Data`, and free-text `Error`.
- Controllers map service failures inconsistently.
- Many services return raw `ex.Message` to API clients.
- `ExceptionMiddleware` returns exception message and stack trace in Development.
- `appsettings.json` contains local connection string, JWT secret, and Gmail SMTP credentials.
- `Program.cs` sets `RequireHttpsMetadata = false`; acceptable for local demo, not for production.
- `AccountController` still has demo/test endpoints: `admin-dashboard` and `publish-article`.

## 2. Design Decisions

- Public registration must never accept `RoleId`.
- Public registration should keep assigning only the default `Member` role.
- `ActorType` is profile metadata only. It must not be used as authorization.
- Admin role assignment must stay inside admin-only APIs.
- Keep search/filter APIs as `GET` with query params for non-sensitive search data.
- Do not switch search filters to `POST` only because query params are visible.
- Auth hardening should be implemented in small phases because it touches DB schema, token contract, frontend contract, and Swagger testing.
- Error contract, rate limiting, and secret management are now higher priority than refresh token because the demo is over and the next goal is system quality.
- Refresh token remains important but should be implemented as its own phase.
- Teacher-reviewed registration flow: collect profile information first, email verification second, password setup last.
- Password should not be accepted on the first register page.
- A registration should not become a real active account until password setup succeeds.
- Preferred implementation for the new registration flow is a separate `PendingRegistrations` table instead of inserting incomplete users with fake/null passwords.

## 2.1 Teacher-Reviewed Registration Flow

Implementation status:

- Paused and rolled back at user request because the schema/API change affects the team workflow.
- No `PendingRegistrations` migration should be applied in the current code state.
- Current backend remains on the legacy register flow: password is submitted in `POST /api/Account/register`, then email verification activates the user.
- Keep this section as a future design option only.

Target frontend flow:

```text
Register page 1
-> user enters email, full name, date of birth, phone number, actor type
-> backend checks email format and existing account
-> backend stores pending registration
-> backend sends email with setup-password link
-> user opens email link
-> frontend shows setup password page
-> frontend submits token + password
-> backend creates real User, assigns default Member role, marks account active
-> user can login
```

Swagger/backend-only testing flow before UI exists:

```text
POST /api/account/register/start
-> copy token from email link
-> POST /api/account/register/complete with token + password
-> POST /api/account/login
```

Recommended API contract:

```text
POST /api/account/register/start
Body:
{
  "email": "student@example.com",
  "fullName": "Student One",
  "dateOfBirth": "2000-01-01",
  "phoneNumber": "0900000000",
  "actorType": "Student"
}

POST /api/account/register/complete
Body:
{
  "token": "email-token-from-link",
  "password": "newPassword123"
}
```

Optional UI helper endpoint:

```text
GET /api/account/register/token-status?token=...
```

Use this only if the frontend needs to validate the token before rendering the password form. Otherwise `register/complete` is enough.

### Database Impact

This flow should add a new table instead of changing `Users.PasswordHash` to nullable.

Recommended table:

```text
PendingRegistrations
- PendingRegistrationId bigint identity primary key
- Email nvarchar(255) not null
- FullName nvarchar(150) null
- DateOfBirth datetime2 null
- PhoneNumber nvarchar(20) null
- ActorType nvarchar(50) not null
- TokenHash nvarchar(255) not null
- CreatedAt datetime2 not null
- ExpiresAt datetime2 not null
- CompletedAt datetime2 null
```

Recommended indexes:

```text
UX_PendingRegistrations_Email_Active
  unique filtered index on Email where CompletedAt is null

UX_PendingRegistrations_TokenHash
  unique index on TokenHash

IX_PendingRegistrations_ExpiresAt
  index on ExpiresAt
```

Reasoning:

- `Users.PasswordHash` is currently required and should stay required for real users.
- `EmailVerificationTokens` currently depends on `UserId`, so it is not a natural fit before user creation.
- Keeping pending registration separate avoids fake password hashes and avoids creating half-real accounts.
- This is easier for teammates to pull because it adds a new table instead of rewriting existing `Users` columns and older migrations.

### Pull/Migration Notes For Team

If this phase is implemented later, teammates should:

```powershell
cd C:\SWP391\SWP391
git pull origin dev
dotnet restore
dotnet ef database update --project .\SWP391\SWP391.csproj --startup-project .\SWP391\SWP391.csproj
dotnet build SWP391.sln
```

Important branch-safety rules if this work is resumed:

- Do not edit old migration files.
- Add one new migration for `PendingRegistrations` only after the team agrees on the workflow and DB impact.
- Keep old register endpoint only temporarily if frontend or Swagger examples still depend on it.
- Prefer adding new request DTOs and new service methods rather than rewriting unrelated auth code.
- Document the new migration name in this file after it is created.

## 3. Recommended Priority

### Phase 0 - Documentation Cleanup

Goal: keep project memory accurate before code changes.

- [x] Create this roadmap.
- [ ] Update `missingOfRegister.md` to remove outdated claims.
- [ ] Mark completed items clearly: no public `RoleId`, email verification exists, logout/revoke exists.
- [ ] Keep remaining gaps: refresh token, forgot/reset password, generic login failure, rate limit, secrets.

### Phase 1 - API Error Contract

Goal: stop leaking implementation details and make frontend responses predictable.

First implementation pass status:

- Added `ApiErrorResponse`.
- Added shared `ErrorCodes`.
- Extended `ServiceResult<T>` with `ErrorCode` and `StatusCode`.
- Added controller helper extensions for standardized error responses.
- Applied the new error response shape to `AccountController`.
- Updated `ExceptionMiddleware` to return a safe generic `INTERNAL_ERROR` response.

Recommended shape:

```json
{
  "success": false,
  "code": "VALIDATION_ERROR",
  "message": "Email is required.",
  "traceId": "00-..."
}
```

Work items:

- [x] Add common API response/error models.
- [x] Extend `ServiceResult<T>` with `ErrorCode` and `StatusCode`, or add a separate mapping layer.
- [x] Add common controller helper/extension to convert `ServiceResult<T>` into `IActionResult`.
- [ ] Stop returning raw `ex.Message` in service failures.
- [ ] Keep detailed exception messages only in logs.
- [x] Update `ExceptionMiddleware` to return a safe generic response even in Development, or at least avoid exposing stack trace through normal API responses.
- [ ] Normalize auth errors to `401`, forbidden errors to `403`, validation to `400`, not found to `404`, conflict to `409`, unexpected errors to `500`.

Suggested error codes:

```text
VALIDATION_ERROR
INVALID_CREDENTIALS
EMAIL_NOT_VERIFIED
RESOURCE_NOT_FOUND
RESOURCE_CONFLICT
UNAUTHORIZED
FORBIDDEN
RATE_LIMITED
INTERNAL_ERROR
```

### Phase 2 - Auth Endpoint Hardening

Goal: make login/register safer without changing DB schema heavily.

Work items:

- [x] Change login response for missing user and wrong password to generic `Invalid email or password.`
- [x] Decide whether unverified email should return a specific `EMAIL_NOT_VERIFIED` response after credentials are proven valid.
- [ ] Avoid logging sensitive login details.
- [ ] Add `POST /api/account/resend-verification-email`.
- [ ] Prevent unlimited verification email creation.
- [ ] Make register SMTP behavior safer: if email send fails after user creation, return a controlled error and allow resend.
- [ ] Remove or relocate `admin-dashboard` and `publish-article` demo endpoints.
- [ ] Review `profile`: either make it a real profile API or rename/comment it as claims debug endpoint.

### Phase 3 - Rate Limiting And Abuse Protection

Goal: reduce brute force, enumeration, and email spam risk.

Work items:

- [ ] Add ASP.NET Core rate limiter in `Program.cs`.
- [ ] Apply stricter policies to login, register, forgot-password, reset-password, and resend-verification-email.
- [ ] Add IP-based throttling for unauthenticated auth endpoints.
- [ ] Consider per-email throttling for verification and forgot-password flows.
- [ ] Consider failed login counter / temporary account lockout after repeated failures.

Suggested starting policy:

```text
login: 5 attempts per minute per IP
register: 3 attempts per minute per IP
resend verification: 3 attempts per 15 minutes per email/IP
forgot password: 3 attempts per 15 minutes per email/IP
```

### Phase 4 - Secret Management

Goal: remove sensitive values from committed config.

Work items:

- [ ] Rotate the exposed Gmail app password.
- [ ] Rotate the JWT secret.
- [ ] Move `JWT:Secret` to user secrets or environment variables.
- [ ] Move SMTP username/password to user secrets or environment variables.
- [ ] Keep only safe defaults in `appsettings.json`.
- [ ] Keep local-only values in `appsettings.Development.json` or user secrets.
- [ ] Document required environment variables for deployment.

Important: do not delete local config blindly if teammates depend on it. Make the migration explicit and documented.

### Phase 5 - Account Lifecycle

Goal: complete expected user account features.

Work items:

- [ ] Add password reset token table or reusable token model.
- [ ] Add `POST /api/account/forgot-password`.
- [ ] Add `POST /api/account/reset-password`.
- [ ] Add `POST /api/account/change-password`.
- [ ] Add `GET /api/account/profile`.
- [ ] Add `PUT /api/account/profile`.
- [ ] Hash reset tokens before storing them.
- [ ] Expire reset tokens quickly.
- [ ] Mark reset tokens as used after successful reset.

### Phase 6 - Refresh Token

Goal: replace long-lived access token with short access token + rotating refresh token.

Recommended model:

```text
Access token: 15-30 minutes
Refresh token: random opaque token, stored hashed in DB
Refresh token rotation: issue a new refresh token on every refresh
Logout: revoke current refresh session
```

Work items:

- [ ] Add `RefreshToken` entity.
- [ ] Add migration.
- [ ] Store only refresh token hash.
- [ ] Add issued/expiry/revoked/replaced metadata.
- [ ] Add `POST /api/account/refresh-token`.
- [ ] Update login response to return access token, access expiration, refresh token, and user summary.
- [ ] Reduce access token lifetime from 7 days to 15-30 minutes.
- [ ] Decide whether refresh token is returned in response body or HTTP-only cookie.
- [ ] Update logout to revoke refresh token/session.
- [ ] Add cleanup job for expired revoked/refresh tokens.

## 4. Implementation Notes

- Keep Controller -> Service -> Repository rule.
- Do not use EF directly in controllers.
- Prefer focused commits/phases; do not rewrite all controllers at once unless the error contract refactor requires mechanical changes.
- Start with auth/account endpoints for error contract, then expand to dashboard/trends/report/admin.
- Add tests if a test project is introduced; otherwise verify with build + Swagger/Postman cases.
- Be careful with existing untracked Memory files.

## 5. Suggested First Coding Session

Recommended first implementation target:

```text
Phase 1 + the safe parts of Phase 2:
- common API error response
- generic login failure
- remove raw exception details from auth-facing paths
- add resend verification email if scope remains manageable
```

Reason:

- It improves production posture immediately.
- It has lower DB blast radius than refresh token.
- It gives frontend a cleaner contract before more auth endpoints are added.

## 6. Definition Of Done For Next Pass

- `dotnet build SWP391.sln` succeeds.
- Swagger still loads.
- Register still creates inactive user and sends verification email.
- Verify email activates user.
- Login returns token for valid active user.
- Login returns a safe generic response for invalid credentials.
- Logout revokes token.
- Error responses follow the agreed response shape for touched endpoints.
- Secrets plan is documented before actual config changes.
