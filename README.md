# LibrarySystem

A library management system built in ASP.NET Core MVC (.NET 10) — admin item CRUD, reception borrowing/returns/fines, manager statistics dashboards, and a public mobile-responsive catalog search.

## Seeded accounts

| Role | Email | Password |
|---|---|---|
| Admin | `admin@library.com` | `Admin@123` |
| Reception | `reception@library.com` | `Reception@123` |
| Manager | `manager@library.com` | `Manager@123` |

## Prerequisites

- **.NET 10 SDK**
- **SQL Server LocalDB** (installed automatically with Visual Studio's "ASP.NET and web development" workload, or standalone via the [SQL Server Express installer](https://www.microsoft.com/sql-server/sql-server-downloads))
- **Visual Studio 2022, latest version** — the solution uses the `.slnx` format, which needs a recent VS 2022 build for native support. Not on Visual Studio? `dotnet run` from the project folder works identically.

## Running it

1. Open `LibrarySystem.slnx`.
2. Press **F5**.

That's it. On first run the app creates the LocalDB database, applies all migrations, seeds three role accounts, and seeds ~20 items, 5 borrowers and a mix of loans (including an overdue one and both paid and unpaid fines) — no Package Manager Console, no manual `Update-Database`, no manual SQL.

## What it does

| Role | Capability |
|---|---|
| **Admin** | Full CRUD over books, music and toys, each with their own type-specific fields |
| **Reception** | Borrow/return by library code with automatic fine calculation, borrower CRUD, an overdue-items report, outstanding-fine payment recording, and per-borrower loan history |
| **Manager** | Read-only dashboards — borrowing, item status and fine statistics, charted with Chart.js |
| **Public** | Search the catalog by name/description/type/availability, no login required, usable on a phone |

## Tech stack

ASP.NET Core MVC · EF Core (Code First) · SQL Server LocalDB · ASP.NET Core Identity (role-based auth) · Bootstrap 5 · Chart.js (vendored locally, not CDN)

## Documentation

- [`docs/PROJECT_REPORT.md`](docs/PROJECT_REPORT.md) — every module and how it fits together, plus the design reasoning and research behind the key decisions (submitted as `submission/Project Report.docx`)
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md) — dated build journal (submitted as `submission/Development Log.docx`)
