# Pharmacy Inventory & Dispensing System

Pharmacy Inventory & Dispensing System manages medicines as batches with expiry dates, dispenses prescriptions atomically against available stock, and tracks inventory adjustments with a full audit trail.

Built with **.NET 10** and **Angular 22**, the project follows Clean Architecture with Vertical Slices (CQRS + MediatR) and a standalone Angular SPA. The API is documented via Scalar/OpenAPI and the frontend supports EN/AR with RTL.

For the detailed architecture, stack, and implementation references, see **[docs/ARCHITECTURE.md](docs/ARCHITECTURE.md)**.

## Overview

This is a production-shaped pharmacy backend and SPA:

* Medicines, variants, and batches are versioned with expiry and stock levels.
* Prescriptions are created by doctors and dispensed by pharmacists with atomic stock reduction and concurrency handling.
* Every change is audited (who/when/old→new values) and soft deletes never leak through queries.
* Security uses short-lived JWTs and rotated httpOnly refresh tokens, with policy-based authorization.

## Tech Stack

| Component | Technology | Version |
|---|---|---|
| Backend | .NET | 10.0.x |
| Web Framework | ASP.NET Core Web API | 10.0.x |
| Language | C# | 14 |
| ORM | Entity Framework Core | 10.0.x |
| Database | SQL Server | 2022 |
| Caching | Redis + OutputCache | 7-alpine |
| Frontend | Angular | 22.1.x |
| UI Library | Angular Material | 22.1.x |
| Language | TypeScript | 5.9+ |
| Realtime | SignalR | 10.x |
| Logging | Serilog | 10.x |

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (`net10.0` as in `src/WebApi/WebApi.csproj`)
- [Node.js 22 LTS](https://nodejs.org/) (see `client/angular-app/package.json` `packageManager: npm@11.12.1`)
- [Docker](https://docs.docker.com/get-docker/) & [Docker Compose](https://docs.docker.com/compose/install/)
- EF Core tools (optional, for migrations): `dotnet tool install --global dotnet-ef`

### 1. Clone & Environment Setup

```bash
git clone https://github.com/ahmadacs/PharmacySystem.git
cd PharmacySystem
```

Create a local secrets file for Docker and local development. The compose file reads `SA_PASSWORD` and `JWT_SIGNING_KEY` from environment (`docker-compose.yml:24,49`):

```bash
# PowerShell (Windows)
Copy-Item .env.example .env  # if .env.example exists; otherwise create .env with:
# SA_PASSWORD=YourStrong@Passw0rd123
# JWT_SIGNING_KEY=your-very-long-secret-key-at-least-32-characters-for-jwt-signing
```

An example `.env` is already present in the repository for local development:

```ini
SA_PASSWORD=YourStrong@Passw0rd123
JWT_SIGNING_KEY=your-very-long-secret-key-at-least-32-characters-for-jwt-signing
```

For local runs without Docker, you can also use user-secrets:

```bash
dotnet user-secrets init --project src/WebApi
dotnet user-secrets set "Jwt:SigningKey" "a-long-random-string-at-least-32-characters" --project src/WebApi
```

> Default values in `src/WebApi/appsettings.json` are `Server=localhost\SQLEXPRESS` (LocalDB), `Redis: localhost:6379`, `Jwt:SigningKey: dev-only-signing-key-change-me-in-production-...`, `Jwt:Issuer: PharmacySystem`, `Jwt:Audience: PharmacySystemClients`, and `Cors:AllowedOrigins: ["http://localhost:4200"]`.

### 2. Start Infrastructure (DB + Redis)

With Docker (recommended — matches `docker-compose.yml`):

```bash
docker compose up --build
```

This starts:

- `db` — SQL Server 2022 on `localhost:1433` (`ACCEPT_EULA=Y`, healthcheck via `sqlcmd -S localhost -U sa -P "$SA_PASSWORD" -Q "SELECT 1" -b -C`)
- `redis` — Redis 7-alpine on `localhost:6379` (`redis-cli ping`)
- `api` — ASP.NET Core Web API built from `./src/WebApi/Dockerfile`, `ASPNETCORE_ENVIRONMENT=Docker`, `ASPNETCORE_URLS=http://+:8080`, exposed as `localhost:5066 -> 8080`
- `angular` — Angular prod build served by nginx on `localhost:4200` (`4200:80`), proxying `/api` → `api:8080` and `/hubs` → `api:8080/hubs`

Health probes: `db`/`redis` are checked before `api` starts; `api` checks `curl -f http://localhost:8080/health` before `angular` starts.

Without Docker, you can use an existing SQL Server / LocalDB and Redis instance and override connection strings via `appsettings.json` or environment variables.

### 3. Apply Database Migrations

Migrations are committed in `src/Infrastructure/Migrations/` and run automatically on startup via `InitializeDatabaseAsync()` in `src/WebApi/Program.cs:248`. To apply manually:

```bash
dotnet ef database update --project src/Infrastructure --startup-project src/WebApi
```

To create a new migration:

```bash
dotnet ef migrations add <Name> --project src/Infrastructure --startup-project src/WebApi
```

### 4. Seeding

Seeding is **automatic and idempotent** (`src/Infrastructure/Seeding/DbSeeder.cs` invoked from `InitializeDatabaseAsync`). On first run it creates:

- 3 roles, 3 users (Admin / Pharmacist / Doctor), and generic/medicine/batch/patient sample data
- No manual step required — just start the API or run `dotnet ef database update` followed by `dotnet run`

Seeded accounts:

| Role | Email | Password |
|---|---|---|
| Admin | admin@pharmacy.com | Admin@1234 |
| Pharmacist | pharmacist@pharmacy.com | Pharma@1234 |
| Doctor | doctor@pharmacy.com | Doctor@1234 |

### 5. Run the Backend API

With Docker: already running at `http://localhost:5066` (see compose `ports: "5066:8080"`).

Without Docker:

```bash
dotnet run --project src/WebApi
```

The API listens on (from `src/WebApi/Properties/launchSettings.json`):

- `http` profile: `http://localhost:5066`
- `https` profile: `https://localhost:7060;http://localhost:5066`

API docs: `http://localhost:5066/scalar` (or `/swagger` fallback), OpenAPI JSON at `http://localhost:5066/openapi/v1.json`, health at `http://localhost:5066/health`.

### 6. Run the Frontend

```bash
cd client/angular-app
npm install
npm start
# or: ng serve
```

This runs `ng serve` (see `package.json` `scripts.start`) on `http://localhost:4200` — the origin allowed by `Cors:AllowedOrigins` in `appsettings.json`.

Other scripts from `package.json`:

```bash
npm run build   # ng build --project angular-app --configuration production
npm test        # ng test (Vitest 4.0.8, TypeScript 6.0.2)
npm run watch   # ng build --watch
```

The API URL is `http://localhost:5066/api/v1` in `client/angular-app/src/environments/environment.ts` (`environment.prod.ts` swaps via `angular.json` file replacements).

### 7. Access the App

- Frontend: http://localhost:4200 (login with seeded accounts above)
- API docs: http://localhost:5066/scalar
- Health: http://localhost:5066/health

## Running Tests

```bash
# Backend (all solutions)
dotnet test

# Frontend
cd client/angular-app
npm test
```

Vitest (`4.0.8`) is used for the Angular project (`package.json` `devDependencies`), with `jsdom`. Tests are run via `ng test`.

## Folder Structure

```text
PharmacySystem/
├── src/
│   ├── Domain/           # Entities, enums, value objects, domain events, exceptions
│   ├── Application/      # CQRS slices (Commands/Queries), DTOs, interfaces, validation
│   ├── Infrastructure/   # EF Core, Identity, JWT, Redis, SignalR, file storage, migrations
│   └── WebApi/           # Controllers, middleware, Program.cs, appsettings.json
├── client/angular-app/   # Standalone, zoneless, signals, Angular Material
│   ├── src/app/core/     # Interceptors, guards, services
│   ├── src/app/shared/   # Components, directives, pipes
│   ├── src/app/features/ # auth, medicines, inventory, prescriptions, dispensing, users, dashboard
│   └── src/app/layout/   # Shell, navigation
├── docs/
│   ├── ARCHITECTURE.md   # Detailed architecture & implementation reference
│   ├── Pharmacy System.pdf # Pre-built PDF (archived)
│   └── screenshots/      # Dashboard, inventory, etc.
├── docker-compose.yml    # API (5066:8080), Angular (4200:80), DB (1433), Redis (6379)
└── PharmacySystem.slnx
```

## Contributing

1. Create a feature branch: `git checkout -b feature/your-feature`
2. Commit with conventional messages: `feat: ...`, `fix: ...`, `docs: ...`
3. Ensure `dotnet build` and `ng build` pass
4. Open a pull request — see the Docker Compose setup above for full-stack verification

## License

MIT — see `LICENSE` if present. Otherwise, all rights reserved to the project owner.

## Known Issues

- Refresh cookie is `Secure` — plain HTTP localhost will not persist the refresh flow; use the `https` profile (`https://localhost:7060`) for full auth flow.
- Registration is open by design (per requirements); restrict to admin in production if needed.
- API currently uses dev URL `http://localhost:5066/api/v1` in `environment.ts`; update `environment.prod.ts` for production.
- Frontend bundle exceeds the 600 kB budget (~791 kB) — expected with Material + SignalR + Translate.
