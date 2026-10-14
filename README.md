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
| **API client** | Read-only `GET /api/v1/*` endpoints (items, categories, branches, status) for an external integration, authenticated with an API key - see [below](#public-read-only-api-v1) |

## Tech stack

ASP.NET Core MVC · EF Core (Code First) · SQL Server LocalDB · ASP.NET Core Identity (role-based auth) · Bootstrap 5 · Chart.js (vendored locally, not CDN) · Swashbuckle/Swagger (API docs, Development only)

## Public read-only API (v1)

A small REST API for the catalogue, separate from the MVC site - `GET` only, no sign-in, meant for another client/integration to read catalogue data. Lives under `Controllers/Api` and `Models/Dto`, not the MVC controllers/view models above.

### Authentication

Every endpoint requires an `X-Api-Key` header. The dev key is in `appsettings.json` under `ApiKey:Key`:

```
dev-key-f3a9c2d1-library-api
```

A missing or wrong key returns `401` in the same JSON shape every other API error uses (see below) - no HTML, no redirect. This is a single shared key suitable for a demo, same spirit as the seeded Identity passwords in `Data/IdentitySeed.cs` - a real deployment would pull it from a secret store (Key Vault, user-secrets, an environment variable) instead of committing it to `appsettings.json`.

### Trying it out

**Swagger UI** - run the app in Development and open **`/swagger`**. Click **Authorize** (top right), paste in the dev key above, click **Authorize** then **Close** - every "Try it out" call after that carries the header automatically. Swagger UI is Development-only; it isn't mapped when the app runs in any other environment.

**curl** - every example below includes the header; swap in your own key if you've changed it in config.

### Error shape

Every error response (model-binding failures, a missing resource, an unhandled exception) is the same shape, never a stack trace:

```json
{ "statusCode": 400, "message": "..." }
```

A `400` from an invalid query parameter names every field that failed to bind, not just the first:

```json
{ "statusCode": 400, "message": "page: The value 'abc' is not valid. | type: The value 'Bad' is not valid." }
```

### Endpoints

#### `GET /api/v1/items` - paged catalogue search

All query parameters are optional and combine with AND.

| Param | Type | Default | Notes |
|---|---|---|---|
| `search` | string | - | Matched against item name and description (substring, case-insensitive) |
| `type` | `Book` \| `Music` \| `Toy` | - | Restricts to one item type |
| `branch` | int | - | Restricts to items currently at this branch (internal branch id - see `/api/v1/branches` for the list, though that returns codes, not ids; omit this filter unless you already know the id) |
| `availableOnly` | bool | `false` | Only items with status `Available` |
| `page` | int | `1` | 1-based; values below 1 are clamped to 1 |
| `pageSize` | int | `20` | Clamped to `[1, 100]` |

```bash
curl "http://localhost:5105/api/v1/items?search=hobbit&pageSize=1" \
  -H "X-Api-Key: dev-key-f3a9c2d1-library-api"
```

```json
{
  "results": [
    {
      "libraryCode": "BK-001",
      "name": "The Hobbit",
      "description": "A reluctant hobbit sets out on an unexpected journey.",
      "type": "Book",
      "status": "Borrowed",
      "summary": "J.R.R. Tolkien - Fantasy",
      "currentBranchName": "Casuarina Library",
      "queueCount": 2
    }
  ],
  "totalCount": 1,
  "page": 1,
  "pageSize": 1
}
```

#### `GET /api/v1/items/{libraryCode}` - one item's full detail

`libraryCode` is a route segment, e.g. `BK-001`. Returns `404` if no item has that code. Unlike the list endpoint, the type-specific fields (`author`/`genre`, `artist`/`year`, `toyType`/`recommendedAge`) are included directly - only the ones matching the item's actual type are non-null.

```bash
curl "http://localhost:5105/api/v1/items/BK-001" \
  -H "X-Api-Key: dev-key-f3a9c2d1-library-api"
```

```json
{
  "libraryCode": "BK-001",
  "name": "The Hobbit",
  "description": "A reluctant hobbit sets out on an unexpected journey.",
  "type": "Book",
  "status": "Borrowed",
  "currentBranchName": "Casuarina Library",
  "queueCount": 2,
  "author": "J.R.R. Tolkien",
  "genre": "Fantasy",
  "artist": null,
  "year": null,
  "toyType": null,
  "recommendedAge": null
}
```

Not found:

```bash
curl "http://localhost:5105/api/v1/items/NOPE" \
  -H "X-Api-Key: dev-key-f3a9c2d1-library-api"
# 404
```

```json
{ "statusCode": 404, "message": "No item found with library code 'NOPE'." }
```

#### `GET /api/v1/categories` - item types with counts, plus genres and toy types

No query parameters.

```bash
curl "http://localhost:5105/api/v1/categories" \
  -H "X-Api-Key: dev-key-f3a9c2d1-library-api"
```

```json
{
  "types": [
    { "type": "Book", "count": 7 },
    { "type": "Music", "count": 7 },
    { "type": "Toy", "count": 6 }
  ],
  "bookGenres": ["Classic", "Dystopian", "Fantasy", "Romance", "Science", "Science Fiction"],
  "toyTypes": ["Board Game", "Construction", "Plush", "Puzzle", "Vehicle"]
}
```

#### `GET /api/v1/branches` - active branches

No query parameters. Deactivated branches are never included.

```bash
curl "http://localhost:5105/api/v1/branches" \
  -H "X-Api-Key: dev-key-f3a9c2d1-library-api"
```

```json
[
  {
    "code": "CAS",
    "name": "Casuarina Library",
    "address": "Building Yellow 7, Casuarina Square, Trower Rd, Casuarina NT 0810",
    "phone": "08 8920 4001",
    "openingHours": "Mon-Fri 9:30am-6pm, Sat-Sun 10am-4pm"
  }
]
```

#### `GET /api/v1/status` - operating status

No query parameters. `serverTime` is library-local (via `ILibraryClock`), not UTC.

```bash
curl "http://localhost:5105/api/v1/status" \
  -H "X-Api-Key: dev-key-f3a9c2d1-library-api"
```

```json
{
  "branchCount": 3,
  "itemsAvailable": 15,
  "apiVersion": "1.0",
  "serverTime": "2026-10-14T14:10:16.0638187"
}
```

### What's deliberately not in the DTOs

The API never serializes EF entities directly - every response is a hand-built DTO (`Models/Dto`), so there's no `Loans`/`Borrower` navigation property for a response to leak data through by accident. Specifically left out: the internal numeric `Id` (the public identifier is `libraryCode` or `code`, not a row id), home-branch/transfer bookkeeping (`homeBranchName`, "away from home"), and anything reservation-identity-level - `queueCount` is a number, never the list of waiting borrowers.

## Documentation

- [`docs/PROJECT_REPORT.md`](docs/PROJECT_REPORT.md) — every module and how it fits together, plus the design reasoning and research behind the key decisions (submitted as `submission/Project Report.docx`)
- [`docs/DEVELOPMENT_LOG.md`](docs/DEVELOPMENT_LOG.md) — dated build journal (submitted as `submission/Development Log.docx`)
