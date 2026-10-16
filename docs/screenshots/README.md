# Screenshots

Captured live against a running instance of the app (Casuarina branch). Each entry lists what the screenshot shows and which requirement or feature it's evidence for.

## Public and kiosk

**01-public-catalog-all-items.jpg** - The public catalogue at `/Catalog`, logged out (nav shows Register/Login, not a user session), listing all 20 seeded items with their type, availability status, library code and home branch. Evidences the public, unauthenticated catalogue browsing requirement.

**02-public-catalog-search-results.jpg** - The same catalogue with the search term "harry" entered, filtered down to a single matching result ("Harry Potter and the Philosopher's Stone"). Evidences the catalogue's free-text search.

**03-public-catalog-branch-filter-palmerston.jpg** - The catalogue with the Branch filter set to "Palmerston Library", showing only the 6 items held at that branch. Evidences per-branch filtering in the public catalogue.

**04-public-catalog-hobbit-queue-count.jpg** - A search for "hobbit" showing The Hobbit's catalogue card with a "2 waiting" queue-count badge and its Borrowed status. Evidences that the public catalogue surfaces live reservation-queue depth per item, not just availability.

**05-kiosk-idle-screen.jpg** - The self-service kiosk idle screen at `/kiosk`, showing the branch name (Casuarina Library) and the three entry points: Check Out, My Account, Search. Evidences the kiosk shell/idle-screen requirement.

**06-kiosk-search-results.jpg** - The kiosk's on-screen keyboard search for "THE", returning 8 results each with a "Place a Hold" action. Evidences kiosk catalogue search built for touch input (large on-screen keyboard, no physical keyboard assumed).

**07-kiosk-hold-confirmation.jpg** - The confirmation screen after placing a hold on Dune (Borrowed, BK-004) from the kiosk search results: "'Dune' reserved for Brian Smith - position 1 in the queue." Evidences that the kiosk's hold-placement flow confirms success and tells the patron their queue position, without needing staff involvement.

## Reception (reception@library.com)

**08-reception-transfer-requested-outgoing.jpg** - The Transfers page (Active filter) right after raising a new transfer: Chess Set (TY-005), an Available item at Casuarina, requested to move to Palmerston Library with the reason "Requested by Palmerston - demand for board games ahead of school holidays". It shows under Outgoing with status Requested and the "Mark In Transit" action visible (only the source branch, Casuarina, can act on it at this stage). Evidences the live transfer-request workflow.

**09-reception-transfer-completed-history.jpg** - The same Transfers page with the Completed filter applied, showing the seeded historical transfer: "Harry Potter and the Philosopher's Stone" (BK-007), moved from Darwin City Library to Casuarina, reason "Rebalancing stock - high demand at Casuarina", status Completed. Evidences that completed transfer history is retained and filterable, alongside the live Requested transfer above.

**10-reception-reservations-hobbit-queue.jpg** - The Reservations index, showing The Hobbit's two-person queue: Brian Smith at position 1 and Carla Diaz at position 2, both Waiting, with placement dates. Evidences the reservation queue-position tracking requirement.

**11-reception-borrow-refusal-on-hold.jpg** - An attempt to borrow The Hobbit (BK-001) for Carla Diaz, refused with "'The Hobbit' is on hold for another borrower's reservation." This item had just been returned (see "Database state" below), which promoted Brian Smith's reservation to Ready and held the item for him specifically - Carla, despite being in the same queue, isn't the person it's currently held for. Evidences the borrow workflow's hold-respecting logic (`LoanService.BorrowAsync`, `Services/LoanService.cs:53-56`).

**12-reception-borrow-success-reservation-fulfilled.jpg** - Immediately after, borrowing the same item for Brian Smith (the correct, Ready reservation holder) succeeds: "'The Hobbit' borrowed by Brian Smith. Due back 29/10/2026. Reservation fulfilled." Evidences that fulfilling a reservation is a first-class outcome of an ordinary borrow, not a separate workflow.

## Manager (manager@library.com)

**13-manager-dashboard.jpg** - The Manager Dashboard, Borrowing tab, All Branches: active/overdue loan counts, a "Loans Over Time" bar chart, a "Most Borrowed Items" bar chart, and the Overdue Loans table. Evidences the statistics dashboard requirement.

**14-manager-notification-sweep-result.jpg** - The result banner after clicking "Run Notification Sweep": "Notification sweep complete: 1 due-soon and 1 overdue notification(s) sent, 0 already notified today, 0 failed. Reservations: 0 expired, 0 promoted to the next person in queue." Evidences the on-demand due-date/overdue notification sweep and reservation-expiry sweep running together as one manager action.

**15-manager-notifications-dashboard.jpg** - The Notifications list (`/Notifications`), showing 6 sent notifications: the due-soon/overdue pair from the sweep above (15/10/2026 5:10 PM), the Borrowed and ReservationReady SMS notifications generated by the Hobbit borrow/return just performed, and the original due-soon/overdue pair from 14/10/2026. Evidences the notification audit log, including both Email and SMS channels and correct local (Darwin, UTC+9:30) timestamps.

**16-manager-borrowing-report-filtered.jpg** - The Borrowing Report with a custom date range (1/09/2026 - 15/10/2026) and the Branch filter set to Casuarina Library applied, showing totals, most-borrowed items and loans-by-branch scoped to that filter. Evidences the manager reporting requirement's date-range and branch filtering (CSV/PDF export buttons also visible, unexercised here).

## Admin (admin@library.com)

**17-admin-csv-import-preview.jpg** - The Import Preview page after uploading the repo's own sample CSV (`wwwroot/samples/item-import-sample.csv`): 6 valid rows (checked, ready to confirm) spanning all three item types and three branches, and 2 invalid rows with their specific rejection reasons ("Unknown branch code 'ZZZ'" and "Year must be a whole number between 1900 and 2100 for Music"). The import was **not** confirmed, so no catalog data was created by this screenshot. Evidences the CSV bulk-import validation requirement.

**18-admin-swagger-ui.jpg** - Swagger UI at `/swagger`, listing the public read-only API v1's endpoint groups (Branches, Categories, Items, Status) with their descriptions and the Authorize control for the API key. Evidences the documented public API requirement.

## Database state left behind

Four things changed data during this capture session:

1. **Placed a hold** on Dune (BK-004, Borrowed) from the kiosk (for screenshot 07), for borrower Brian Smith - "'Dune' reserved for Brian Smith - position 1 in the queue." This is a new, active reservation that didn't exist before.

2. **Raised a transfer**: Chess Set (TY-005) requested from Casuarina to Palmerston Library, reason "Requested by Palmerston - demand for board games ahead of school holidays" (for screenshot 08). It's still sitting as **Requested** - nobody marked it in transit or completed it, so Chess Set's `CurrentBranchId` is unchanged (still Casuarina) and it still shows as Available there. This is a new, live transfer row that didn't exist before.

3. **Returned and re-borrowed The Hobbit** (BK-001), to set up screenshots 11 and 12: returning it promoted Brian Smith's reservation from Waiting (position 1) to Ready; borrowing it for Carla Diaz was refused (screenshot 11, no state change); borrowing it for Brian Smith then succeeded (screenshot 12), creating a new loan (due 29/10/2026), marking Brian's reservation Collected, and leaving Carla Diaz as the sole remaining queue entry, now at position 1, still Waiting.

4. **Ran a Manager notification sweep** (screenshot 14), which sent 1 due-soon and 1 overdue notification (both already due regardless of the sweep - it only sends once per day per borrower, so running it again today will report "already notified" rather than resending).

Net effect on the seed data:
- **Dune (BK-004)**: now has one active reservation - Brian Smith, position 1, Waiting - on top of its existing loan to Carla Diaz (overdue, unchanged).
- **Chess Set (TY-005)**: has one open, Requested transfer to Palmerston Library. Still physically at Casuarina and still Available to borrow/transfer-cancel.
- **The Hobbit (BK-001)**: now on loan to Brian Smith instead of its original borrower, due 29/10/2026. Carla Diaz is the only person left waiting for it.
- Nothing else (other catalog items, other loans, fines) was changed. The CSV import preview (screenshot 17) was never confirmed, so it created no items.

Also worth noting: an early attempt at the Completed-transfer screenshot used the harness's `form_input` to set the Status dropdown to "Completed," but that only sets the value - it doesn't fire the `change` event the page's `onchange="this.form.submit()"` listens for, so the page never actually reloaded with the filter applied. That's why an earlier draft of screenshot 09 showed an empty queue: it was still showing the Active filter, not Completed. The current screenshot 09 was captured by navigating directly to `/Transfers?status=Completed` instead, which correctly shows the seeded Harry Potter transfer.
