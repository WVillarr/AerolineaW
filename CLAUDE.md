# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

ASP.NET Core 7 Web API for an airline / control-tower domain (airplanes, airports, passengers, tickets). Swagger UI is the only client. All user-facing API messages are in Spanish.

## Environment caveat: net7.0 vs installed runtimes

All three projects target `net7.0`, but this machine has only the .NET 8 and 9 runtimes (SDK 9.0.203). Consequences:

- `dotnet build` works (the SDK builds net7.0 fine).
- **`dotnet run` and `dotnet ef` fail** with "You must install or update .NET to run this application" unless roll-forward is enabled.

Workaround (verified working — runs the app on .NET 8):

```powershell
$env:DOTNET_ROLL_FORWARD="Major"
```

Set it once per shell before any `dotnet run` / `dotnet ef` command. The alternative is installing the .NET 7 runtime.

## Commands

The solution lives at `Src/AerolineaW/AerolineaW.sln`, **not** at the repo root. Run from the repo root:

```powershell
dotnet build Src/AerolineaW/AerolineaW.sln
```

```powershell
dotnet run --project Src/AerolineaW/AerolineaW.csproj --launch-profile https
```

Swagger is at `/swagger` and is registered **only when `ASPNETCORE_ENVIRONMENT=Development`** (set by the launch profiles). Profiles: `http` (5129), `https` (7054/5129), `IIS Express`.

EF Core: the `DataBase` context and migrations live in `Infractructure`, but the connection string lives in the API project, so both `--project` and `--startup-project` are required. `Src/AerolineaW/` contains two project files, so pass explicit `.csproj` paths:

```powershell
dotnet ef migrations list --project Src/Infractructure/Infractructure.csproj --startup-project Src/AerolineaW/AerolineaW.csproj
```

```powershell
dotnet ef database update --project Src/Infractructure/Infractructure.csproj --startup-project Src/AerolineaW/AerolineaW.csproj
```

Migrations are **not** applied at startup — the database must be updated manually. Only one migration exists: `20230811185019_DB`.

There are no test projects; `dotnet test` finds nothing.

## Architecture

Three projects, referenced in a straight line — `AerolineaW` → `Infractructure` → `Domain`. Note the folder/namespace is misspelled `Infractructure` throughout; keep the spelling when adding files.

- **`Domain`** — entities (`Domain.Modelo`), DTOs (`Domain.DTOS`), and *all* service interfaces (`Domain.Interface`). No EF or ASP.NET dependencies beyond FluentValidation.
- **`Infractructure`** — `DataBase` (the `DbContext`), the service implementations, FluentValidation validators, and Bogus fake-data generators.
- **`AerolineaW`** — controllers, `Program.cs`, and `Configuration/DependencyInjection.cs`.

### Request flow

Controller → `IValidationManager` → `I*Services` → `DataBase`. Every controller constructor injects `DataBase`, its `I*Services`, and `IValidationManager`, though `_dbContext` is generally unused in the controller itself.

DI registration is centralized in `Src/AerolineaW/Configuration/DependencyInjection.cs` via the `GetDependencyInjections()` extension; the `DbContext` is registered separately in `Program.cs`. A new service or validator must be added there or it will not resolve.

### Validation

`IValidationManager` / `ValidationManagerService` is a facade that wraps the four `IValidator<TDto>` instances so controllers depend on one interface instead of four. Adding a validated entity means touching all of: the DTO, an `AbstractValidator<TDto>`, a method on `IValidationManager`, and two registrations in `DependencyInjection.cs`.

`TicketValidation` takes `DataBase` in its constructor and uses the async FK-existence rules in `Validation/CustomValidation.cs` (`CheckPassengerExistAsync` etc.) to confirm referenced rows exist.

Controllers surface errors as `BadRequest(result.Errors.ToStringList())` — `ToStringList()` is the extension in `Validation/ValidationError.cs` that flattens `ValidationFailure` to plain message strings.

### Conventions that look like bugs but are load-bearing

- **Services return Spanish status strings, and controllers branch on those literals.** `DeleteAirplane`, `DeleteTicket`, `DeletePassenger`, `DeleteAirport*` return `"Se elimino correctamente"` or `"Hubo un error"`, and each controller does `if (result != "Hubo un error")` to choose `Ok` vs `BadRequest`. Changing a message string silently changes the HTTP status.
- **DTO → entity mapping is Mapster** (`dto.Adapt<Entity>()`) with no explicit config; mapping relies on identical property names. `AirplaneServices.MapAirplaneObject` then sets `Status = "En Tierra"` after adapting, since the DTO has no status. `Airplane.Status` transitions to `"En vuelo"` on ticket creation and `"Aterrizó"` in `TicketServices.ModifyTicket`.
- **Every controller action wraps its body in `try/catch` returning `StatusCode(500, e)`** — the raw exception object is serialized into the response.
- Newtonsoft is used instead of System.Text.Json specifically for `ReferenceLoopHandling.Ignore`, because `Ticket` has bidirectional navigation properties.

### Airport1 / Airport2 — inconsistent arrival/departure meaning

There are two structurally identical airport entities. Which one means "arrival" is **not consistent** across the codebase:

- `Ticket` maps `AirportArrivalId` → `Airport1` and `AirportDepartureId` → `Airport2`.
- `AirportServices` treats `Airport1` as **departure** and `Airport2` as **arrival**.
- `CustomValidation.CheckAirportArrivalAsync` queries `Airport2`; `CheckAiportDepartureAsync` queries `Airport1`.
- `TicketServices.MapTicketObject` looks up `AirportArrivalId` in `Airport1`.

Check which table a given path actually touches before changing anything here; do not assume the property name.

### Fake data

`GET /Dummy?increment=N` (route is `[controller]` with no `api/` prefix, unlike every other controller) generates N rows per table via Bogus. `DummyController` news up `DummyDataServices` by hand — it is not registered in DI. `FakeSetup.FticketSetup` picks random existing `Passenger`/`Airplane`/`Airport1`/`Airport2` rows, so `DatosDummy` must run its steps in order and those tables must be populated first.

## Configuration

`Src/AerolineaW/appsettings.json` holds `ConnectionStrings:AirplaneApiConnectionString`, hardcoded to `DESKTOP-OVIVB88\SQLEXPRESS` / database `torredecontrol` with `Trusted_Connection=True`. It must be pointed at a local SQL Server before anything that touches the database will work. `Microsoft.EntityFrameworkCore.InMemory` is referenced but not wired up.

`docker-compose.dcproj` and the Docker launch profiles exist, but there is no `Dockerfile`, `docker-compose.yml`, or `.dockerignore`, and the `.dcproj` is not part of the `.sln` — the Docker path is non-functional.
