# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build and run

- Restore + build: `dotnet build Abril-Backend.csproj`
- Run (Development profile, port 5236 http / 7298 https): `dotnet run --project Abril-Backend.csproj`
- Target framework is `net10.0` (ASP.NET Core). There is no test project in this repo — do not invent `dotnet test` invocations.
- Dockerfile builds with the .NET 10 SDK and exposes port 8080.
- Swagger is wired but only mounted in Development at `/swagger`.

## EF Core migrations

```bash
# Add a migration (run from the project root) — this part is fine to run locally
dotnet ef migrations add <MigrationName> --project Abril-Backend.csproj
```

**Do not run `dotnet ef database update` against the real environments.** Applying migrations is done manually: generate the migration's SQL (`dotnet ef migrations script`) and apply it directly via SQL/pgAdmin against the target database. `Migrations_Manual/` holds hand-written SQL for changes that didn't go through an EF migration at all — check there before assuming a schema change must be an EF migration.

The connection string used comes from whichever `appsettings.*.json` / `appsettings.Local.json` is active. Run migrations against the target provider before deploying. Both providers share one `AppDbContext`; if PG overrides are needed, add them in `ConfigurePostgreSQL` before generating the migration.

## Configuration model

`Program.cs` layers config as `appsettings.json` → `appsettings.{Environment}.json` → `appsettings.Local.json` → env vars. `appsettings.Development.json`, `appsettings.Production.json`, and `appsettings.Local.json` are all gitignored and contain real secrets (DB, JWT key, Azure Blob, SendGrid, PowerAutomate webhook, Reniec/Sunat tokens, Azure AD). **Do not commit these** and do not paste their contents into chat/PRs.

Provider selectors in config drive DI:
- `Database:DatabaseProvider` → `"SqlServer"` or `"PostgreSQL"`. PostgreSQL path uses `UseSnakeCaseNamingConvention()` and `AppDbContext` has a `ConfigurePostgreSQL` branch with explicit column/table overrides (e.g. `User` → `app_user`, `Phase.Order` → `phase_order`). When adding entities or properties whose names collide with PG reserved words or snake-cased conventions, add an override there.
- `Email:EmailProvider` → `"SendGrid"` | `"PowerAutomate"` | (default) SMTP.
- `Storage:StorageProvider` → `"Azure"` (Blob) | (default) Local filesystem under `wwwroot/uploads`. `IStorageContainerResolver` resolves logical container names (lessons, ivts, cuaderno-de-obra, etc.) from `StorageOptions`.

## Architecture: two coexisting patterns

The codebase mixes a **traditional layered** layout with a newer **vertical-slice Features** layout. New work generally goes under `Features/`; older/shared domains still live at the repo root.

### Traditional layered (root folders)

```
Controllers/                 -> [ApiController] classes, route "api/v1/[controller]" or explicit kebab-case
Application/
  Interfaces/                -> I*Service abstractions
  Services/                  -> *Service implementations (business logic, thin wrappers over repos)
  DTOs/                      -> Request/response shapes, grouped by domain folder
  Exceptions/AbrilException  -> Domain exception carrying HTTP StatusCode
Infrastructure/
  Interfaces/I*Repository    -> Repository abstractions
  Repositories/              -> EF Core implementations, use IDbContextFactory<AppDbContext>
  Models/                    -> EF entity classes
Shared/
  Data/AppContext.cs         -> AppDbContext (note: despite folder name, namespace is Abril_Backend.Infrastructure.Data)
  Services/                  -> Cross-cutting: Email, Excel, Jwt, Reniec, Storage, Sunat
  Models/                    -> Cross-cutting entities (Company*, etc.)
```

### Vertical slice (Features/)

The nesting depth varies by module — three flavors coexist, don't "fix" one to match another:

```
# Most common: Module -> Features/ -> <Feature>Feature -> layers
Features/<Module>Module/
  <Module>Module.cs          -> static class with AddXxxModule(this IServiceCollection) DI registration
  Features/<Feature>Feature/
    Application/{Interfaces,Services,Dtos}
    Infrastructure/{Interfaces,Repositories[,Models]}
    Presentation/*Controller.cs

# No extra "Features/" wrapper (SsomaModule, BoletinModule): Module -> <Feature>Feature -> layers directly
Features/SsomaModule/<Feature>Feature/{Application,Infrastructure,Presentation}

# Single-feature module, no per-feature subfolder (EvaluacionesModule, HabilitacionModule):
Features/<Module>Module/{Application,Infrastructure,Presentation}
```

Modules wired in `Program.cs` (each `AddXxxModule` call is the only thing registered there — keep internal DI inside the module file, not in `Program.cs`):

- `AuthModule` — internal JWT login, `MicrosoftLoginFeature`/`MicrosoftProfileFeature` (Entra ID), `RoleFeature`, `UserFeature`, `ContractorCredentialsFeature`. (Supersedes the old standalone "MicrosoftAuthModule" name — it's a feature inside `AuthModule` now.)
- `ContractorsModule` — `ContractorRegistrationFeature`, `ContractorManagementFeature`.
- `CostsModule` — `AdjudicacionesFeature`, `CronogramaFeature`, `Configuration`.
- `ConfigurationModule` — `AreaFeature`, `HolidayFeature`, `ProjectFeature`.
- `SsomaModule` (largest — flat, no `Features/` wrapper) — `SaludOcupacionalFeature`, `MiSaludFeature`, `AccidentesIncidentesFeature`, `AmonestacionesFeature`, `AuditoriaAtsFeature`, `CharlasFeature`, `ChecklistFeature`, `DesempenoSupervisorFeature`, `HorasHombreFeature`, `IndicadoresProactivosFeature`, `InspeccionFeature`, `OptFeature`, `PasoFeature`, `PresupuestoMaterialesFeature`, `ProyectoHabilitadoFeature`, `RacFeature`.
- `GestionAdministrativaModule` — `SolicitudSalidasFeature`, `GestionSalidasFeature`, `LugaresFeature`, `MotivosSalidaFeature`, `TrayectosFeature`, `RevisorSalidasFeature`, `VisibilidadSalidasFeature` (viáticos/movilidad: solicitud → aprobación → rendición workflow, mirrored by the frontend's `gestion-administrativa/features/{solicitud-salidas,gestion-salidas}`).
- `HabilitacionModule` (flat, no per-feature subfolder) — worker onboarding (SCTR/VidaLey, EMO, company assignment).
- `EvaluacionesModule` (flat, no per-feature subfolder) — evaluations.
- `UnidadDeProyectosModule` — `ActasReunionFeature`, `CronogramaActividades`, `MilestoneScheduleFeature`, `ProjectsDashboard`.
- `MejoraContinuaModule` — `LessonsLearnedFeature`, `LessonsDashboardFeature`, `Configuracion`.
- `VecinosModule` — `GestionVecinosFeature`, `ControlLicenciasFeature`, `CroquisFeature`.
- `AccountingModule` — `InvoicesFeature`, `Configuration`.
- `BoletinModule` (flat) — `BirthdayClubFeature`.

`ArquitecturaComercial` stays in the traditional layer (see below), not under `Features/`, despite being a large active domain.

SSOMA entities use the `Ss*` prefix (e.g. `SsClinica`, `SsMedicoOcupacional`, `SsEmoTipo`, `SsEmoExamenDetalle`, `SsInterconsulta`, `SsProgramacionEmo`). These are distinct from the `Worker*` entities (`Worker`, `WorkerEmo`, `WorkerEmoConvalidacion`, `WorkerVinculacion`) which are shared across AC and SSOMA domains.

Feature entities are still registered as `DbSet`s on the single shared `AppDbContext` in `Shared/Data/AppContext.cs`. When adding a feature entity, add the `DbSet` there and (if PG needs overrides) extend `ConfigurePostgreSQL`.

## Conventions to preserve

- **Repositories use `IDbContextFactory<AppDbContext>`** and open a short-lived context per call (`using var ctx = _factory.CreateDbContext()`), not an injected `AppDbContext`. Follow this pattern for new repos — several repos run parallel queries and rely on distinct contexts. Some older traditional-layer repos (e.g. `AreaRepository`, `PersonRepository`) are registered directly as concrete types without an interface (`builder.Services.AddScoped<AreaRepository>()`) and injected by concrete type in controllers — this is a legacy pattern; new repos should use the interface-backed pattern.
- **Controllers wrap calls in try/catch** with this shape: catch `AbrilException` → return `StatusCode(ex.StatusCode, new { message = ex.Message })`; catch generic `Exception` → return 500 with a fixed Spanish message. Throw `AbrilException(message, statusCode)` from services/repos for expected failures; let unexpected ones bubble to the 500 handler.
- **Auth**: two JWT schemes are registered — `"Bearer"` (internal JWT signed with `Jwt:Key`) and `"AzureAd"` (Microsoft Entra). The default authorization policy accepts both. Endpoints are authenticated by default unless `[AllowAnonymous]` is used. `NameClaimType` is `ClaimTypes.NameIdentifier` for internal JWT — read the current user id via `User.FindFirst(ClaimTypes.NameIdentifier)`.
- **Rate limiting**: the `sunat-ruc` policy (5/hour per IP for unauthenticated callers, unlimited for authenticated) is configured in `Program.cs`. Apply with `[EnableRateLimiting("sunat-ruc")]` on relevant endpoints.
- **Mixed domain vocabulary**: older English-named entities (`Project`, `User`, `Stage`) coexist with newer Spanish-named ones introduced for the Arquitectura Comercial domain (`Proyecto`, `Worker`, `Empresa`, `AcActividad`, `AcEtapa`, `AcActividadPlantilla`) and SSOMA (`Ss*` entities). These refer to different tables — do not assume a Spanish name is an alias for an English one. Check `AppDbContext` and existing queries before cross-referencing.
- **ArquitecturaComercial lives in the traditional layer**, not under `Features/`: service at `Application/Services/ArquitecturaComercialService.cs`, repo at `Infrastructure/Repositories/ArquitecturaComercialRepository.cs`. Its entities (`AcActividad`, `AcEtapa`, `AcActividadPlantilla`, `AcCategoria`, `AcEspecialidad`, `Proyecto`, `Empresa`) all carry `Ac*` or Spanish names.
- **User-facing messages** in error responses are in Spanish; keep that tone for new endpoints.

## Pitfalls

- `Abril_Backend.sln` is `.gitignore`d (`*.sln`). Regenerating it locally is fine; committing it is not.
- `wwwroot/uploads` is the local storage fallback; do not commit user-uploaded artifacts.
- The `obj/` and `bin/` folders are gitignored but present locally — avoid scanning into them for source.
- When adding a `DbSet`, PG column-name collisions (reserved words, case, `order`/`phase_order`-style reshaping) must be handled in `ConfigurePostgreSQL` or queries will fail only on the PostgreSQL provider.
- `Migracionfinal/` (one-off Python data-migration scripts + source spreadsheets) and `Migrations_Manual/` (hand-written SQL not generated by EF) are historical/manual artifacts, not part of the app's runtime or the `Migrations/` EF pipeline — don't treat them as current schema source of truth.
