# Servitore 2026

A professional, multi-user service-management system: WPF desktop clients talking to a
central ASP.NET Core API, with EF Core, SignalR real-time sync, barcode/QR scanning, and
WhatsApp notifications.

This package is a **complete, ready-to-open Visual Studio solution scaffold** — every
project, folder, and file from the planned structure has been created with working
boilerplate (DbContext, entities, JWT auth, controllers, services, repositories,
SignalR hub, MVVM ViewModels, WPF views) so you can open it and start filling in
business logic immediately, instead of setting up plumbing from scratch.

## Solution layout

```
Servitore.sln
│
├── Servitore.Desktop    WPF (.NET 8) + Material Design — the UI
├── Servitore.API         ASP.NET Core 8 Web API — business logic, auth, SignalR
├── Servitore.Database     EF Core — entities, DbContext, migrations
├── Servitore.Shared       Models/enums/constants shared by Desktop + API
└── Servitore.Reports       PDF/Excel export service + report templates
```

## Getting started

1. **Open `Servitore.sln`** in Visual Studio 2022 (17.8+) with the ".NET desktop
   development" and "ASP.NET and web development" workloads installed.
2. **Restore NuGet packages** — VS will do this automatically, or run:
   ```
   dotnet restore
   ```
3. **Configure the database** in `Servitore.API/appsettings.json`
   (`ConnectionStrings:DefaultConnection`), then create the initial migration:
   ```
   cd Servitore.API
   dotnet ef migrations add InitialCreate --project ../Servitore.Database
   dotnet ef database update --project ../Servitore.Database
   ```
4. **Set a real JWT signing key** in `appsettings.json` (`Jwt:Key`) — the placeholder
   value must be replaced before running anywhere outside your own machine.
5. **Run the API** (`Servitore.API`) first, then **run the Desktop app**
   (`Servitore.Desktop`) — update `AppConstants.DefaultApiBaseUrl` in
   `Servitore.Shared/Constants/AppConstants.cs` if your API isn't on
   `https://localhost:5001`.
6. **Create your first Admin user.** There's no public registration endpoint by
   design — seed an Admin directly in the database (hash the password with
   `BCrypt.Net.BCrypt.HashPassword(...)`) so you can log in and take it from there.

## What's implemented vs. stubbed

| Area | Status |
|---|---|
| Solution/project structure, references | ✅ Complete |
| Database entities, DbContext, configurations, seed roles | ✅ Complete |
| JWT auth (login, token issuing, `[Authorize]`) | ✅ Working |
| Customers, Users, Service Tickets — full CRUD (API + repos + services) | ✅ Working |
| SignalR real-time ticket notifications (create/update broadcasts to all desktops) | ✅ Working |
| WhatsApp notifications on ticket create/update/complete | ⚠️ Stub — wire your provider's API URL/key into `appsettings.json` → `WhatsApp` section |
| Barcode/QR generation (API) | ⚠️ ZXing wired up; PNG encoding step needs an imaging library (SkiaSharp/System.Drawing) — see `BarcodeService.cs` |
| Reports (PDF/Excel export) | ⚠️ Stub — pick FastReport/QuestPDF/ClosedXML (see `Servitore.Reports.csproj` comments) and implement `ExportService` |
| Desktop: Login + Dashboard shell with navigation | ✅ Working |
| Desktop: Customers, Assets, Service Tickets, Warranty, AMC, Reports, Settings, User Management screens | ⚠️ Scaffolded `UserControl`s with placeholder content — build out each grid/form against its ViewModel |
| Activity log table | ✅ Entity + DbSet created — wire up writes from your controllers/services as needed |

## Development order (suggested)

This matches the original plan:

1. Database tables → migrations
2. Login API → Login screen
3. Dashboard
4. Customer module
5. Asset module
6. Service Ticket module + Activity Logs
7. SignalR real-time updates (already wired for ticket create/update)
8. Barcode module (finish PNG encoding)
9. WhatsApp module (plug in your provider)
10. Reports (pick a reporting engine)
11. Testing → Deployment

## Notes

- Authentication uses JWT bearer tokens; SignalR reuses the same token via the
  `access_token` query string convention already configured in `Program.cs`.
- Passwords are hashed with BCrypt (`BCrypt.Net-Next`).
- All cross-cutting models (DTOs the Desktop and API both care about) live in
  `Servitore.Shared` so you never have to duplicate a class between the two apps.
