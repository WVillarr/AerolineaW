# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ASP.NET Core 10 airline operations API — airports, aircraft, flights, bookings. Clean
Architecture across four projects (`Domain` → `Application` → `Infrastructure`/`Api`). Rewritten
from an earlier learning-project version; see [README.md](README.md) for the business rules
table, architecture diagram, and the two deliberate design trade-offs worth knowing before
touching the code (`Application` referencing `Microsoft.EntityFrameworkCore`, and `Booking`'s
looser-than-textbook aggregate boundary).

## Commands

The solution file is `AerolineaW.slnx` at the repo root (the newer XML solution format `dotnet
new sln` defaults to on .NET 10 tooling — not a typo for `.sln`).

```bash
dotnet build
dotnet test                                          # everything: Domain unit tests + Api integration tests
dotnet test tests/AerolineaW.Domain.UnitTests         # fast, no external dependencies
dotnet run --project src/AerolineaW.Api
dotnet format --verify-no-changes                     # what CI runs; matches .editorconfig
```

The integration test project (`tests/AerolineaW.Api.IntegrationTests`) uses Testcontainers and
needs a working Docker daemon — `dotnet test` on just that project will fail with
`DockerUnavailableException` if Docker isn't running, which is an environment problem, not a
code problem. GitHub Actions' `ubuntu-latest` runners have Docker available by default.

EF Core migrations live in `Infrastructure`; `AirlineDbContextFactory`
(`src/AerolineaW.Infrastructure/Persistence/AirlineDbContextFactory.cs`) is an
`IDesignTimeDbContextFactory`, so `dotnet ef` commands only need `--project`, not
`--startup-project`:

```bash
dotnet ef migrations add <Name> --project src/AerolineaW.Infrastructure/AerolineaW.Infrastructure.csproj --output-dir Persistence/Migrations
```

Migrations are **not** applied by that command against a real database automatically — but they
*are* applied automatically at app startup in Development (`DemoDataSeeder.StartAsync` calls
`Database.MigrateAsync()` before seeding), which is a deliberate departure from a more
production-typical "migrate by hand" policy, made so a fresh clone can `docker compose up` and
have a working, seeded database with zero manual steps.

## Environment notes (this machine)

- .NET SDK 10.0.400 is installed via winget alongside the 8.0/9.0 runtimes that were already
  present; nothing here needs the old `DOTNET_ROLL_FORWARD` workaround a net7.0-targeting
  predecessor of this project used to need.
- SQL Server Express LocalDB (`(localdb)\MSSQLLocalDB`) is available and is what
  `appsettings.Development.json` points at by default — use it for anything that needs a real
  SQL Server without spinning up Docker.
- Docker Desktop does not reliably start in this sandbox (its own logs show it starting and
  self-terminating within ~9 seconds — consistent with no nested virtualization available here).
  `docker compose` and the Testcontainers integration suite need an environment where Docker
  actually works; don't assume this one is that environment without checking `docker info` first.

## Architecture

```
src/AerolineaW.Domain/            entities, enums, value objects, domain exceptions — zero package references
src/AerolineaW.Application/       DTOs (records), one service + FluentValidation validator per aggregate, IApplicationDbContext
src/AerolineaW.Infrastructure/    AirlineDbContext, EF configurations, migrations, DemoDataSeeder
src/AerolineaW.Api/               controllers, Program.cs, GlobalExceptionHandler, ValidationFilter
tests/AerolineaW.Domain.UnitTests/        82 pure unit tests, one rule per test, no mocks
tests/AerolineaW.Api.IntegrationTests/    WebApplicationFactory<Program> + Testcontainers.MsSql
```

Dependency direction is one-way: `Api` → `Infrastructure`/`Application` → `Domain`. `Domain` has
no dependencies at all — every business rule (seat capacity, payload limits, status transitions,
booking/check-in windows) lives in `Flight`/`Booking`'s constructors and methods, not in a
service layer, and is unit-tested without a database in the loop.

### Aggregates and the concurrency mechanism

`Flight` is the aggregate root for booking: `Booking` is only ever constructed via
`Flight.Book(...)`, which is where seat-capacity, payload, seat-uniqueness, and the booking-cutoff
window are all enforced. `Flight.SeatsBooked` is a *stored* counter (not `Bookings.Count`
computed on read) specifically so every `Book()` call issues an `UPDATE` against the `Flight`
row — paired with a `rowversion` EF Core concurrency token
(`FlightConfiguration.cs`), that's what makes two concurrent bookings for the last seat produce a
real `DbUpdateConcurrencyException` instead of silently overselling. `BookingService.CreateAsync`
retries up to three times on that exception, clearing the change tracker and re-reading
everything fresh each attempt — see the comments there before changing the retry logic.

### Adding a new validated request

FluentValidation validators are auto-discovered via `AddValidatorsFromAssembly` in
`Application/DependencyInjection.cs` — no manual registration needed, unlike the old project's
`IValidationManager` facade (removed). Add the validator class in the same file as its DTOs and
it's picked up automatically; `ValidationFilter` (`Api/Validation/ValidationFilter.cs`) is what
actually invokes it per-request and turns failures into a `ValidationProblemDetails` 400.

### Error handling

`GlobalExceptionHandler` (`Api/ErrorHandling/GlobalExceptionHandler.cs`) is the only place an
exception becomes an HTTP response: `NotFoundException` → 404, any `DomainException` subtype →
409, an exhausted-retry `DbUpdateConcurrencyException` → 409, `ArgumentException` → 400,
everything else → 500 with a generic message (logged server-side, never serialized to the
client). New domain rules should throw a `DomainException` subtype (see
`Domain/Exceptions/`) rather than a generic exception, so they get mapped correctly without
touching the handler.

## Configuration

`ConnectionStrings:AirlineDatabase` has no value in the base `appsettings.json` on purpose — the
app throws a clear `InvalidOperationException` at startup if it's missing rather than silently
falling back to something. `appsettings.Development.json` supplies the LocalDB connection string
for local dev; `docker-compose.yml` supplies it via the `ConnectionStrings__AirlineDatabase`
environment variable, sourced from `.env` (gitignored; copy `.env.example`).
