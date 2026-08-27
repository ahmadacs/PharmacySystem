# Pharmacy Inventory & Dispensing System — Development Skills

## 1. Project

Build a production-shaped **Pharmacy Inventory & Dispensing System**.

### Main domain

* Medicines
* Medicine batches
* Expiry dates
* Stock levels
* Prescriptions
* Dispensing
* Inventory adjustments
* Users, roles, and permissions

### Roles

* **Admin**
  * Manage medicines, batches, users, and roles
  * Full CRUD access

* **Pharmacist**
  * Dispense prescriptions
  * Adjust stock
  * View inventory
  * Manage dispensing-related operations

* **Doctor**
  * Create prescriptions
  * Manage own prescriptions (resource-based / "own records only" authorization)

### Core business rules

* Expiry alerts for medicine batches
* Low-stock warnings
* Refill eligibility rules
* Dispensing must reduce stock atomically (transactional, safe against concurrent stock changes)
* Prevent dispensing expired medicines
* Validate sufficient stock before dispensing
* Enforce ownership/resource authorization where required (e.g., a Doctor can only manage their own prescriptions)

---

## 2. Mandatory Technology Stack

### Backend

* .NET 10 LTS (do **not** use .NET 11 preview)
* ASP.NET Core Web API
* Entity Framework Core 10.x
* SQL Server 2022+ (LocalDB / Docker / Express)
* C# 14
* MediatR for CQRS
* ASP.NET Core Identity (or a custom user store — must be justified in the README)
* JWT access + refresh tokens
* Serilog (console + rolling file)
* Scalar preferred for API documentation (Swagger UI acceptable fallback)

### Frontend

* Angular 22
* Standalone components only — no NgModules
* Signals
* Zoneless change detection (default in v22)
* Reactive Forms
* **Angular Material** (latest compatible with v22) — do not mix with PrimeNG
* TypeScript 5.9+
* Strict mode, no `any`

---

## 3. Architecture

Use Clean Architecture:

```text
/src
├── Domain        → Entities, Enums, Value Objects, Domain Exceptions (no external deps)
├── Application    → Interfaces, DTOs (Data Annotations), Handlers, Mapping extensions
├── Infrastructure → EF Core DbContext, Repositories, Identity, JWT, Email/File services, Migrations
└── WebApi         → Controllers, Middleware, DI registration, Program.cs

/client
└── angular-app
```

### Dependency rules

```text
Domain
  ↑
Application
  ↑
Infrastructure

WebApi → Application + Infrastructure
```

* Domain has no external dependencies outside the BCL.
* Domain must not reference EF Core, ASP.NET, or any NuGet package outside the BCL.
* Application depends only on Domain.
* Infrastructure depends on Application.
* WebApi depends on Application and Infrastructure.
* Controllers must remain thin: no business logic, no DbContext, no LINQ queries in controllers.
* All cross-layer communication uses Application interfaces.
* Never return entities directly from APIs — always return DTOs/response models.

---

## 4. Backend Patterns

Use:

* CQRS with MediatR — Commands and Queries separated
* Repository + Unit of Work, or DbContext as Unit of Work (must be justified in README if chosen)
* Dependency Injection everywhere — no `new` on services
* Result/Either pattern OR centralized custom exception handling — no scattered try/catch in controllers
* Manual entity ↔ DTO mapping (explicit `ToDto()` / `ToEntity()` extension methods)
* **No AutoMapper**

---

## 5. API Requirements

RESTful APIs, versioned:

```text
/api/v1/...
```

Support on all list endpoints:

* Correct HTTP verbs
* Correct status codes
* Pagination
* Filtering
* Sorting
* Search

Example:

```text
GET /api/v1/medicines?page=1&pageSize=10&search=paracetamol&sortBy=name&sortDir=asc
```

Required status codes where applicable:

```text
200  201  204  400  401  403  404  409  422  500
```

Consistent error response envelope:

```json
{
  "success": false,
  "message": "Validation failed",
  "errors": {
    "name": ["Name is required"]
  },
  "traceId": "..."
}
```

---

## 6. Validation & Cross-Cutting Concerns

Implement:

* Global exception handling using `IExceptionHandler` (.NET 10) — no raw stack traces returned to the client
* Data Annotations on every incoming DTO: `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]`, `[RegularExpression]`, `[Compare]`
* At least one **custom** `ValidationAttribute` for a rule built-ins can't express — e.g. `[FutureDate]` on a batch expiry date, or `[PositiveQuantity]` on stock quantities. Domain/DB-dependent rules (e.g., "batch not already expired at dispense time") belong in the Application layer, not in an attribute.
* Customize `ApiBehaviorOptions.InvalidModelStateResponseFactory` so model validation errors go through the standard error envelope, not the default `ProblemDetails`
* Serilog structured logging with a correlation/trace ID per request
* Rate limiting on authentication endpoints (built-in .NET rate limiter)
* Proper CORS configuration for the Angular origin (no `AllowAnyOrigin` with credentials)
* `/health` endpoint

---

## 7. Database

Use:

* EF Core Code First
* Migration files committed to Git
* Seed data

Seed at minimum:

* Admin role
* Pharmacist role
* Doctor role
* One Admin user (+ credentials documented in README)
* At least one Pharmacist and one Doctor seeded account (so the reviewer can log in as each role)
* Around 20 sample records (medicines, batches, prescriptions, etc.)

All entities must contain:

```text
CreatedBy
CreatedAt
ModifiedBy
ModifiedAt
IsDeleted
```

Implement:

* Automatic audit fields (via `SaveChangesAsync` override or an EF interceptor)
* Soft delete
* Global query filter
* Soft-deleted records must never leak through normal queries

---

## 8. Authentication

Implement:

* Register
* Login
* Logout
* Refresh token
* Change password
* Forgot password (email may be mocked to a log/file)

JWT:

* Short-lived access token: ~15 minutes
* Long-lived refresh token
* Store refresh tokens **hashed** in DB
* Rotate refresh tokens on use
* Revocable refresh tokens

Security:

* ASP.NET Identity password hashing or BCrypt — never plaintext, never MD5/SHA1
* Account lockout after N failed attempts
* **Token storage on the client must be documented** — in-memory access token + httpOnly refresh cookie is the strongest option; if a different approach is chosen, justify it in the README

---

## 9. Authorization / RBAC

Use:

* Policy-based authorization
* Named policies
* Permission claims

Example:

```text
Permissions.Medicines.Create
Permissions.Medicines.Update
Permissions.Medicines.Delete
Permissions.Prescriptions.Create
Permissions.Dispensing.Create
Permissions.Inventory.Adjust
```

Do not scatter `[Authorize(Roles = "Admin")]` throughout the application.

Use resource-based authorization where ownership matters (e.g., a Doctor accessing only their own prescriptions) via `IAuthorizationHandler`.

Authenticated but unauthorized users must receive `403 Forbidden`, not `401`.

Angular UI permission checks are only UX — authorization must always be enforced server-side.

---

## 10. Angular

Use:

* Standalone components
* Signals for state, `computed`, `resource` / `httpResource` where appropriate
* Zoneless
* Lazy-loaded routes: `loadComponent`, `loadChildren`
* Reactive Forms, including at least one dynamic/nested form
* Functional route guards (`CanActivateFn`) for auth + role
* HTTP interceptor: attach token, handle `401` → silent refresh → retry
* A structural directive or signal-based helper for permission-based UI, e.g. `*hasPermission="'Medicines.Create'"`

Structure:

```text
core/      → interceptors, guards, services
shared/    → components, pipes, directives
features/  → per domain
layout/
```

Features should be domain-oriented:

```text
features/
├── auth/
├── medicines/
├── inventory/
├── prescriptions/
├── dispensing/
└── users/
```

Implement:

* Auth interceptor
* Silent refresh on 401
* Route guards
* Permission-based UI
* Loading states (Angular Material skeletons/spinners/progress-bar)
* Empty states
* Global HTTP error handling → Angular Material `MatSnackBar` toast, never a raw error object
* Responsive desktop/mobile UI (Angular CDK Layout / BreakpointObserver)
* Light/dark theme toggle (Angular Material theming — system-level variables)
* Environment-based API URL — no hardcoded `localhost` URLs

Use Angular Material's `MatTable` with server-side pagination/sorting/filtering for all list screens, `MatDialog` for create/edit/confirm flows, and `MatFormField`/`MatInput`/`MatSelect` etc. for Reactive Forms.

---

## 11. Main Screens

Prioritize quality over quantity — a working app with 4 well-built screens scores higher than a broken app with 12 half-built ones.

Recommended screens:

1. Login
2. Dashboard
3. Medicines / Inventory
4. Prescriptions
5. Dispensing
6. Users / Roles if time allows

Important UI on data screens:

* Server-side pagination, sorting, filtering, search
* Dialogs
* Validation messages
* Toasts/snackbars
* Loading states
* Empty states

---

## 12. Pharmacy Business Logic

### Inventory

* Medicine has multiple batches
* Each batch has an expiry date
* Track available quantity
* Detect low stock
* Detect near-expiry/expired batches

### Dispensing

When dispensing:

1. Validate prescription
2. Validate medicine
3. Select valid batch(es)
4. Ensure batch is not expired
5. Ensure sufficient stock
6. Reduce stock atomically
7. Record dispensing transaction
8. Preserve audit information

The operation must be transactional and safe against concurrent stock changes.

### Prescriptions

* Doctor creates prescription
* Pharmacist dispenses prescription
* Validate refill eligibility
* Track prescription status
* Prevent invalid/duplicate dispensing

---

## 13. API Documentation

Use Scalar preferably (or Swagger UI), served at `/scalar/v1` or `/swagger`.

Every endpoint should include:

* Summary
* Parameters
* Request schema
* Response schema
* Possible status codes

Configure JWT Bearer authentication in the API documentation UI so a reviewer can log in and try endpoints directly from the browser.

Include example requests/responses for at least the authentication endpoints.

Also:

* Export OpenAPI JSON and commit it to the repository
* Generate the Angular API client from the OpenAPI spec

---

## 14. Git & GitHub

* One repository containing backend + Angular
* Start Git on day 1
* Push **at least once a day** — do not work locally for a week and upload everything at the end
* Meaningful commits:

```text
feat: add medicine management
feat: implement prescription workflow
feat: add JWT refresh tokens
fix: prevent dispensing expired batches
fix: return 403 for unauthorized users
docs: update setup instructions
```

Avoid meaningless commits such as `update`, `fix`, `test`, `asdf`.

Use feature branches (`feature/login`, `auth-module`, etc.) and merge completed features into `main`.

Never commit `bin/`, `obj/`, `node_modules/`, `.vs/`, secrets, or real connection strings.

---

## 15. README

README must contain:

1. Project title + one-paragraph description
2. Screenshots/GIF
3. Tech stack + versions
4. Architecture overview
5. Architecture diagram (Mermaid is fine)
6. Prerequisites (.NET 10 SDK, Node LTS, SQL Server — exact versions)
7. Backend setup (clone, connection string, user secrets, `dotnet ef database update`, `dotnet run`)
8. Frontend setup (`npm install`, environment config, `ng serve`)
9. Database setup
10. Seeded accounts (Admin / Pharmacist / Doctor + passwords)
11. API documentation URL
12. Folder structure
13. Known issues
14. What could be improved with more time
15. Notes justifying any architectural choice that deviates from the defaults (e.g., DbContext-as-UoW instead of Repository, custom user store instead of Identity, token storage strategy)

A new developer should be able to clone and run the application without asking any questions.

---

## 16. Additional Features

These are part of the required scope — implement all of them alongside the core system (sections 1–15), not as an afterthought.

### Docker
`docker-compose up` should run Angular + .NET API + SQL Server with one command.

### SignalR
Real-time notifications: low-stock, medicine expiry warning, prescription dispensed, new prescription for pharmacists.

### Redis / Output Caching
Use for read-heavy endpoints (medicines, inventory, dashboard statistics, low-stock lists). Do **not** cache data where stale results could break dispensing/business rules.

### File Upload
File type + size validation, storage abstraction. Potential use: prescription attachment, medicine image, doctor prescription document.

### Excel/PDF Export
Inventory report, medicine list, dispensing history, prescription report — both Excel and PDF.

### Localization
English + Arabic, with RTL support. UI must switch direction correctly when Arabic is selected.

### Audit Log Screen
Who changed what, when, what entity, old value, new value.

---

## 17. Quality Rules

Priority order:

1. Correctness
2. Security
3. Clean Architecture
4. Business rules
5. Readable code
6. Maintainability
7. Good UX
8. Documentation

Do not over-engineer. Do not add unnecessary abstractions. Do not generate code that cannot be explained during the demo — **the developer must be able to explain every submitted line of code**; unexplainable code is treated as not written by them.

---

## 18. Source of Truth

This `skills.md` file is the official and complete source of requirements for the **Pharmacy Inventory & Dispensing System**. No external assignment file will be attached — everything the AI coding agent needs must be derived from this document alone.
