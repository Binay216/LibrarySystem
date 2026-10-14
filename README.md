# LibrarySystem

A library management system built in ASP.NET Core MVC (.NET 10), covering a full branch network rather than a single desk: item CRUD and bulk import, reception borrowing/returns/fines, cross-branch transfers, a reservation queue, due-date/overdue notifications, manager statistics and exportable reports, a public catalogue, a public read-only API, and a touchscreen self-service kiosk.

## Quick start

1. **Clone the repository.**
2. Open `LibrarySystem.slnx` in Visual Studio.
3. Press **F5**.

That's it - no Package Manager Console, no manual `Update-Database`, no manual SQL. On first run the app creates the LocalDB database, applies every migration, and seeds:

- 3 branches (Casuarina, Palmerston, Darwin City)
- 3 role accounts (Admin, Reception, Manager - see the table below)
- 5 borrowers, with a mix of loans including an overdue one and both paid and unpaid fines
- ~20 items (7 books, 7 music, 6 toys) across the three branches
- An active reservation queue on one item, so the hold/queue-position features have something to show immediately

Not using Visual Studio? `dotnet run` from the project folder does the same thing. For a clean-machine walkthrough with more detail and a troubleshooting section, see **[docs/INSTALLATION.md](docs/INSTALLATION.md)**.

## Changed the seed data? You must delete the database

This is the single most common point of confusion with this project, and it caught development out more than once - worth reading even if you skip everything else here.

**Seeding only ever runs once, the very first time the database is created.** If you edit `Data/SeedData.cs` or `Data/IdentitySeed.cs` - different items, a different branch, a different password - and just press F5 again, **nothing changes**. EF Core sees the database already exists, skips seeding entirely, and you keep looking at the old data wondering why your edit didn't take.

To make a seed change take effect, delete the database so it gets created (and seeded) fresh on the next run:

1. In Visual Studio, open **View → SQL Server Object Explorer**.
2. Expand `(localdb)\MSSQLLocalDB` → **Databases**.
3. Find the database named `aspnet-LibrarySystem-...` (the exact name is in `appsettings.json` under `ConnectionStrings:DefaultConnection`).
4. Right-click it → **Delete**. Tick "Close existing connections" if asked.
5. Press F5. The app recreates and reseeds the database from scratch.

(Full step-by-step, plus the command-line alternative, is in [docs/INSTALLATION.md](docs/INSTALLATION.md#troubleshooting).)

## Seeded accounts

| Role | Email | Password |
|---|---|---|
| Admin | `admin@library.com` | `Admin@123` |
| Reception | `reception@library.com` | `Reception@123` |
| Manager | `manager@library.com` | `Manager@123` |

Patrons (borrowers) don't have a login at all - there's no public registration or patron password anywhere in the system. The self-service kiosk identifies a patron by a card number, which **is** the borrower's internal ID (1 through 5 for the seeded borrowers - Reception's Borrowers list shows the full names). See [Kiosk](#kiosk) below, and the "Borrower identification" remarks on `KioskController` for why that's a reasonable shortcut for a demo but not what a real deployment should do.

## Which branch is this running as?

Each running instance of the app operates as exactly one branch, set in `appsettings.json`:

```json
"Library": {
  "BranchCode": "CAS"
}
```

Valid codes (from the seed data): `CAS` (Casuarina), `PAL` (Palmerston), `DRW` (Darwin City). Change the value and restart the app - the app checks this at startup and **refuses to start** with a clear error naming the valid codes if it doesn't match a seeded branch, so a typo here fails loudly rather than silently.

**What visibly changes when you switch branch:**

- The branch badge in the top-right of the navbar, and the branch name shown on the Manager dashboard's default view.
- **Reception → Borrow**: an item can only be borrowed if it's physically at *this* branch right now - the error message names which branch it's actually at if not.
- **Reception → Transfers**: the whole queue is scoped to this branch - "incoming" and "outgoing" are relative to it, and only the source branch of a transfer can mark it in transit.
- **Reception → Overdue**: only shows loans that were borrowed *at this branch*.
- **Kiosk** (`/kiosk`): the idle screen's branch name, and which items can be checked out there (same rule as Reception → Borrow).

**What does *not* change:**

- **Manager dashboards and reports** default to every branch, with an optional filter to narrow to one - they don't follow the server's own configured branch.
- **Reception → Return** accepts a return from any branch (a cross-branch return is legitimate and reconciled by an automatic transfer request back home).
- **The public catalogue and API** always search across all branches; a branch filter there is something the *visitor* picks, independent of which branch the server itself is configured as.
- **The notification sweep** processes due-soon/overdue reminders for every branch at once.
- **Admin bulk import** (CSV or the metadata search) lets the admin choose any branch per item, regardless of which branch the server is running as.

## Navigating each module

| Role | Area | Where | What it's for |
|---|---|---|---|
| Admin | Items | `/Items` | Full CRUD over books, music and toys, each with their own type-specific fields |
| Admin | Import | `/Import` | Bulk-create items from a CSV upload (preview → confirm), or search a mock external metadata source and import one record at a time |
| Reception | Borrow / Return | `/Reception` | Borrow or return by library code, with automatic fine calculation on return |
| Reception | Overdue | `/Reception/Overdue` | Loans from this branch still out past their due date |
| Reception | Fines | `/Reception/Fines` | Outstanding fines, with a "mark paid" action |
| Reception | Borrowers | `/Borrowers` | Borrower CRUD and per-borrower loan history |
| Reception | Transfers | `/Transfers` | Raise, track and receive item transfers to/from other branches |
| Reception | Reservations | `/Reservations` | Place a reservation for a borrower, look up an item's queue, cancel a reservation |
| Manager | Dashboard | `/Manager` | Borrowing, item status and fine statistics, charted, filterable by branch |
| Manager | Reports | `/Reports/Borrowing`, `/Reports/FineRevenue`, `/Reports/InventoryHealth` | The same three statistics as detailed, date-ranged reports, each exportable as CSV or PDF |
| Manager | Notifications | `/Notifications` | Read-only log of every due-soon/overdue/ready-for-pickup notification that's been sent |
| Public (no login) | Home / Catalog | `/`, `/Catalog` | Browse and search the catalogue across every branch |
| Public (no login) | Kiosk | `/kiosk` | The self-service touchscreen terminal - see below |
| API client | API v1 | `/api/v1/*` | Read-only catalogue data for an external integration - see [below](#public-read-only-api-v1) |

## Testing the integrations

A few features are easy to miss because they're not on the obvious happy path of "log in and click Items." Here's how to actually exercise each one.

### The API

Run the app in Development and open **`/swagger`**. Click **Authorize**, paste in the dev key from `appsettings.json` (`ApiKey:Key`, currently `dev-key-f3a9c2d1-library-api`), then use "Try it out" on any endpoint - the key is carried automatically after that. Full endpoint reference is in the [API section](#public-read-only-api-v1) below.

### CSV import

Log in as Admin, go to **Import**, and click **Download Sample CSV** right there on the page - it's a real file committed to the repo (`wwwroot/samples/item-import-sample.csv`), with eight rows covering all three item types and two deliberately broken ones (an unknown branch code, a non-numeric year) so you can see the validation work without crafting your own bad data. Upload it, review the valid/invalid split, confirm, and check the items landed in `/Items`.

### Metadata search import

Same **Import** page, second tab. Search for a title or ISBN - try "mockingbird" or "9781451673319" - pick a result, choose a home branch and a library code, and import it. The ~30 records it searches are bundled in the app itself (no internet access needed).

### Notification sweep

Log in as Manager, go to the **Dashboard**, and click **Run Notification Sweep**. It queues due-soon/overdue reminders (due-soon meaning due within 3 days) and expires any stale reservation holds, then reports what it did. Check **Notifications** afterward to see what was generated - the seed data deliberately has both: Carla Diaz's loan of "Dune" is already overdue, and Emma Wilson's loan of "Rumours" is due in 2 days, so the first sweep after a fresh seed always queues one of each.

### Reservation queue

Two ways to see it:

- **Staff side**: Reception → **Reservations** → Place Reservation, against an item that's already Borrowed (the seed data's "The Hobbit" already has two people queued). Placing another adds a third position; cancelling one renumbers everyone behind it.
- **Patron side**: search the catalogue (web or kiosk) for an unavailable item - it shows "N waiting" if anyone's queued. At the kiosk, "Place a Hold" on that result walks through entering a card number and joining the queue.

### Kiosk

Just open **`/kiosk`** - no login. It's a self-contained, full-screen experience (no site navigation) designed for a 1024×768 touch terminal. From the idle screen:

- **Check Out**: enter a card number (try `4` - David Chen, who has an outstanding fine but no current loan, so there's nothing to conflict with), then one or more library codes. Try an available code (e.g. `BK-007`) for a successful checkout, and a borrowed one (e.g. `BK-001`) to see the "place a hold instead" offer.
- **My Account**: enter a card number to see current loans (overdue flagged), outstanding fines, and active reservations with queue positions. Try `3` (Carla Diaz) for a borrower with all three.
- **Search**: browse the catalogue and place a hold on an unavailable result without needing to already be identified.

Every screen has a visible Cancel/Start Over control, and the session resets to idle after 60 seconds of inactivity, clearing whatever card number was entered.

## Tech stack

ASP.NET Core MVC · EF Core (Code First) · SQL Server LocalDB · ASP.NET Core Identity (role-based auth) · Bootstrap 5 · Chart.js (vendored locally, not CDN) · QuestPDF (PDF export) · Swashbuckle/Swagger (API docs, Development only)

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

- [`docs/INSTALLATION.md`](docs/INSTALLATION.md) - standalone setup guide for a clean machine, with a troubleshooting section
- [`docs/USER-GUIDE.md`](docs/USER-GUIDE.md) - patron-facing guide to searching the catalogue, on the web and at the kiosk
