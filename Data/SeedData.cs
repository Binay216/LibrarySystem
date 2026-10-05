using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Services;

namespace LibrarySystem.Data
{
    /// <summary>
    /// Seeds sample branches, items, borrowers, loans and transfers so the app is
    /// demonstrable on first run without any manual steps. The loan mix deliberately
    /// includes an overdue loan, both paid and unpaid fines, and a few items
    /// borrowed more than once, so the manager dashboards and the reception
    /// overdue/fines/history reports never show up empty or flat. One item's home
    /// and current branch deliberately differ, with a matching open Transfer request
    /// moving it back, and a second, already-completed Transfer gives the status
    /// filter something to show on both sides. Guarded by an existing-items check,
    /// so it only runs once - changing this data requires dropping and re-migrating
    /// the database.
    /// </summary>
    public static class SeedData
    {
        /// <summary>Seeds branches, items, borrowers, loans and transfers if the database is empty; otherwise does nothing.</summary>
        public static void Initialize(ApplicationDbContext context, ILibraryClock clock)
        {
            if (context.Items.Any())
            {
                return;
            }

            var today = clock.Today;

            var branches = new List<Branch>
            {
                new() { Code = "CAS", Name = "Casuarina Library", Address = "Building Yellow 7, Casuarina Square, Trower Rd, Casuarina NT 0810", Phone = "08 8920 4001", OpeningHours = "Mon-Fri 9:30am-6pm, Sat-Sun 10am-4pm", IsActive = true },
                new() { Code = "PAL", Name = "Palmerston Library", Address = "Lot 4221 Chung Wah Terrace, Palmerston NT 0830", Phone = "08 8920 4002", OpeningHours = "Mon-Fri 9:30am-6pm, Sat 10am-4pm", IsActive = true },
                new() { Code = "DRW", Name = "Darwin City Library", Address = "Cnr Harry Chan Ave & Bennett St, Darwin NT 0800", Phone = "08 8920 4003", OpeningHours = "Mon-Fri 9am-6pm, Sat-Sun 10am-4pm", IsActive = true },
            };

            context.Branches.AddRange(branches);

            // Save so branches get generated Ids before items reference them.
            context.SaveChanges();

            var casuarina = branches[0];
            var palmerston = branches[1];
            var darwinCity = branches[2];

            var books = new List<Book>
            {
                new() { Name = "The Hobbit", Description = "A reluctant hobbit sets out on an unexpected journey.", LibraryCode = "BK-001", Author = "J.R.R. Tolkien", Genre = "Fantasy", HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
                new() { Name = "1984", Description = "A dystopian vision of a totalitarian surveillance state.", LibraryCode = "BK-002", Author = "George Orwell", Genre = "Dystopian", HomeBranchId = palmerston.Id, CurrentBranchId = palmerston.Id },
                new() { Name = "Pride and Prejudice", Description = "Wit, romance and class in Regency England.", LibraryCode = "BK-003", Author = "Jane Austen", Genre = "Romance", HomeBranchId = darwinCity.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "Dune", Description = "Politics and prophecy on the desert planet Arrakis.", LibraryCode = "BK-004", Author = "Frank Herbert", Genre = "Science Fiction", HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
                // Home and current branch deliberately differ - a Palmerston book
                // presently sitting at Darwin City, e.g. returned to the wrong branch.
                new() { Name = "The Great Gatsby", Description = "Wealth and longing in the Jazz Age.", LibraryCode = "BK-005", Author = "F. Scott Fitzgerald", Genre = "Classic", HomeBranchId = palmerston.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "A Brief History of Time", Description = "An accessible tour of cosmology and physics.", LibraryCode = "BK-006", Author = "Stephen Hawking", Genre = "Science", HomeBranchId = darwinCity.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "Harry Potter and the Philosopher's Stone", Description = "A boy discovers he is a wizard.", LibraryCode = "BK-007", Author = "J.K. Rowling", Genre = "Fantasy", HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
            };

            var music = new List<Music>
            {
                new() { Name = "Abbey Road", Description = "Studio album, the last recorded by the band.", LibraryCode = "MU-001", Artist = "The Beatles", Year = 1969, HomeBranchId = palmerston.Id, CurrentBranchId = palmerston.Id },
                new() { Name = "Thriller", Description = "The best-selling album of all time.", LibraryCode = "MU-002", Artist = "Michael Jackson", Year = 1982, HomeBranchId = darwinCity.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "Back in Black", Description = "Hard rock tribute album.", LibraryCode = "MU-003", Artist = "AC/DC", Year = 1980, HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
                new() { Name = "Rumours", Description = "Soft rock album written amid the band's breakups.", LibraryCode = "MU-004", Artist = "Fleetwood Mac", Year = 1977, HomeBranchId = palmerston.Id, CurrentBranchId = palmerston.Id },
                new() { Name = "The Dark Side of the Moon", Description = "Concept album exploring conflict and mortality.", LibraryCode = "MU-005", Artist = "Pink Floyd", Year = 1973, HomeBranchId = darwinCity.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "21", Description = "Soul-influenced pop album about heartbreak.", LibraryCode = "MU-006", Artist = "Adele", Year = 2011, HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
                new() { Name = "Random Access Memories", Description = "Disco and funk revival album.", LibraryCode = "MU-007", Artist = "Daft Punk", Year = 2013, HomeBranchId = palmerston.Id, CurrentBranchId = palmerston.Id },
            };

            var toys = new List<Toy>
            {
                new() { Name = "Wooden Building Blocks", Description = "50-piece natural wood block set.", LibraryCode = "TY-001", ToyType = "Construction", RecommendedAge = 3, HomeBranchId = darwinCity.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "Remote Control Car", Description = "Battery-powered off-road RC car.", LibraryCode = "TY-002", ToyType = "Vehicle", RecommendedAge = 6, HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
                new() { Name = "Jigsaw Puzzle 500pc", Description = "500-piece landscape jigsaw puzzle.", LibraryCode = "TY-003", ToyType = "Puzzle", RecommendedAge = 8, HomeBranchId = palmerston.Id, CurrentBranchId = palmerston.Id },
                new() { Name = "Plush Teddy Bear", Description = "Soft plush bear, machine washable.", LibraryCode = "TY-004", ToyType = "Plush", RecommendedAge = 0, HomeBranchId = darwinCity.Id, CurrentBranchId = darwinCity.Id },
                new() { Name = "Chess Set", Description = "Folding wooden chess set with weighted pieces.", LibraryCode = "TY-005", ToyType = "Board Game", RecommendedAge = 7, HomeBranchId = casuarina.Id, CurrentBranchId = casuarina.Id },
                new() { Name = "Lego Space Shuttle", Description = "480-piece buildable space shuttle model.", LibraryCode = "TY-006", ToyType = "Construction", RecommendedAge = 9, HomeBranchId = palmerston.Id, CurrentBranchId = palmerston.Id },
            };

            context.Items.AddRange(books);
            context.Items.AddRange(music);
            context.Items.AddRange(toys);

            var borrowers = new List<Borrower>
            {
                new() { FirstName = "Alice", LastName = "Johnson", Email = "alice.johnson@example.com", Phone = "555-0101", Address = "12 Maple St, Springfield", MembershipDate = today.AddYears(-2) },
                new() { FirstName = "Brian", LastName = "Smith", Email = "brian.smith@example.com", Phone = "555-0102", Address = "45 Oak Ave, Springfield", MembershipDate = today.AddYears(-1) },
                new() { FirstName = "Carla", LastName = "Diaz", Email = "carla.diaz@example.com", Phone = "555-0103", Address = "78 Pine Rd, Springfield", MembershipDate = today.AddMonths(-8) },
                new() { FirstName = "David", LastName = "Chen", Email = "david.chen@example.com", Phone = "555-0104", Address = "9 Birch Ln, Springfield", MembershipDate = today.AddMonths(-5) },
                new() { FirstName = "Emma", LastName = "Wilson", Email = "emma.wilson@example.com", Phone = "555-0105", Address = "23 Cedar Ct, Springfield", MembershipDate = today.AddMonths(-3) },
            };

            context.Borrowers.AddRange(borrowers);

            // Save so Items and Borrowers get generated Ids before loans reference them.
            context.SaveChanges();

            var hobbit = books[0];
            var orwell1984 = books[1];
            var prideAndPrejudice = books[2];
            var dune = books[3];
            var greatGatsby = books[4];
            var harryPotter = books[6];
            var abbeyRoad = music[0];
            var thriller = music[1];
            var backInBlack = music[2];
            var woodenBlocks = toys[0];
            var rcCar = toys[1];

            var alice = borrowers[0];
            var brian = borrowers[1];
            var carla = borrowers[2];
            var david = borrowers[3];
            var emma = borrowers[4];

            // IdentitySeed.InitializeAsync runs before SeedData.Initialize (see
            // Program.cs), so the reception account always already exists here.
            var receptionUserId = context.Users.First(u => u.Email == IdentitySeed.ReceptionEmail).Id;

            // Fine figures below match the policy in SPEC.md (14-day loan, $0.50/day
            // late capped at $20, +$10 damaged, +$50 destroyed) so they stay
            // consistent with LoanService.CalculateFine once Phase 3 introduces it.
            // BorrowedAtBranchId is each item's current branch at seed time - every
            // item here has a single, unambiguous branch, so there's no separate
            // "which branch was it actually borrowed from" to track.
            var loans = new List<Loan>
            {
                // Currently out, not yet due.
                new() { Item = hobbit, Borrower = alice, BorrowedAtBranchId = hobbit.CurrentBranchId, BorrowedDate = today.AddDays(-5), DueDate = today.AddDays(9) },
                new() { Item = abbeyRoad, Borrower = brian, BorrowedAtBranchId = abbeyRoad.CurrentBranchId, BorrowedDate = today.AddDays(-2), DueDate = today.AddDays(12) },

                // Currently out and overdue - no fine yet, it is finalised on return.
                new() { Item = dune, Borrower = carla, BorrowedAtBranchId = dune.CurrentBranchId, BorrowedDate = today.AddDays(-20), DueDate = today.AddDays(-6) },

                // Returned on time.
                new() { Item = orwell1984, Borrower = david, BorrowedAtBranchId = orwell1984.CurrentBranchId, BorrowedDate = today.AddDays(-30), DueDate = today.AddDays(-16), ReturnedDate = today.AddDays(-18), FineAmount = 0m, FinePaid = true },
                new() { Item = thriller, Borrower = emma, BorrowedAtBranchId = thriller.CurrentBranchId, BorrowedDate = today.AddDays(-25), DueDate = today.AddDays(-11), ReturnedDate = today.AddDays(-12), FineAmount = 0m, FinePaid = true },

                // Returned late, fine paid.
                new() { Item = prideAndPrejudice, Borrower = alice, BorrowedAtBranchId = prideAndPrejudice.CurrentBranchId, BorrowedDate = today.AddDays(-40), DueDate = today.AddDays(-26), ReturnedDate = today.AddDays(-20), FineAmount = 3.00m, FinePaid = true },

                // Returned late, fine outstanding.
                new() { Item = backInBlack, Borrower = brian, BorrowedAtBranchId = backInBlack.CurrentBranchId, BorrowedDate = today.AddDays(-60), DueDate = today.AddDays(-46), ReturnedDate = today.AddDays(-10), FineAmount = 18.00m, FinePaid = false },

                // Returned damaged: late fee + $10 damage surcharge, outstanding.
                new() { Item = woodenBlocks, Borrower = carla, BorrowedAtBranchId = woodenBlocks.CurrentBranchId, BorrowedDate = today.AddDays(-15), DueDate = today.AddDays(-1), ReturnedDate = today, FineAmount = 10.50m, FinePaid = false },

                // Destroyed: late fee + $50 replacement fee, outstanding.
                new() { Item = rcCar, Borrower = david, BorrowedAtBranchId = rcCar.CurrentBranchId, BorrowedDate = today.AddDays(-45), DueDate = today.AddDays(-31), ReturnedDate = today.AddDays(-25), FineAmount = 53.00m, FinePaid = false },

                // Extra historical on-time returns so a few items have been borrowed
                // more than once - gives the "Most Borrowed Items" chart a real
                // ranking instead of a flat count of 1 across the board. All dated
                // and returned well before that item's loan above, so no two loans
                // for the same item ever overlap.
                new() { Item = hobbit, Borrower = emma, BorrowedAtBranchId = hobbit.CurrentBranchId, BorrowedDate = today.AddDays(-90), DueDate = today.AddDays(-76), ReturnedDate = today.AddDays(-80), FineAmount = 0m, FinePaid = true },
                new() { Item = hobbit, Borrower = carla, BorrowedAtBranchId = hobbit.CurrentBranchId, BorrowedDate = today.AddDays(-70), DueDate = today.AddDays(-56), ReturnedDate = today.AddDays(-58), FineAmount = 0m, FinePaid = true },
                new() { Item = hobbit, Borrower = david, BorrowedAtBranchId = hobbit.CurrentBranchId, BorrowedDate = today.AddDays(-50), DueDate = today.AddDays(-36), ReturnedDate = today.AddDays(-38), FineAmount = 0m, FinePaid = true },
                new() { Item = dune, Borrower = alice, BorrowedAtBranchId = dune.CurrentBranchId, BorrowedDate = today.AddDays(-80), DueDate = today.AddDays(-66), ReturnedDate = today.AddDays(-68), FineAmount = 0m, FinePaid = true },
                new() { Item = dune, Borrower = emma, BorrowedAtBranchId = dune.CurrentBranchId, BorrowedDate = today.AddDays(-55), DueDate = today.AddDays(-41), ReturnedDate = today.AddDays(-43), FineAmount = 0m, FinePaid = true },
                new() { Item = orwell1984, Borrower = brian, BorrowedAtBranchId = orwell1984.CurrentBranchId, BorrowedDate = today.AddDays(-60), DueDate = today.AddDays(-46), ReturnedDate = today.AddDays(-48), FineAmount = 0m, FinePaid = true },
                new() { Item = abbeyRoad, Borrower = carla, BorrowedAtBranchId = abbeyRoad.CurrentBranchId, BorrowedDate = today.AddDays(-45), DueDate = today.AddDays(-31), ReturnedDate = today.AddDays(-33), FineAmount = 0m, FinePaid = true },
            };

            hobbit.Status = ItemStatus.Borrowed;
            abbeyRoad.Status = ItemStatus.Borrowed;
            dune.Status = ItemStatus.Borrowed;
            woodenBlocks.Status = ItemStatus.Damaged;
            rcCar.Status = ItemStatus.Destroyed;

            context.Loans.AddRange(loans);

            var transfers = new List<Transfer>
            {
                // Open: The Great Gatsby heading back to its home branch, matching
                // the Home/Current mismatch seeded on the item itself above.
                new() { Item = greatGatsby, FromBranchId = darwinCity.Id, ToBranchId = palmerston.Id, Status = TransferStatus.Requested, RequestedDate = today.AddDays(-3), RequestedByUserId = receptionUserId, Reason = "Returning to home branch" },

                // Closed: a past transfer that explains why Harry Potter's home and
                // current branch already agree on Casuarina - it moved there and the
                // transfer completed before this data was seeded.
                new() { Item = harryPotter, FromBranchId = darwinCity.Id, ToBranchId = casuarina.Id, Status = TransferStatus.Completed, RequestedDate = today.AddDays(-30), CompletedDate = today.AddDays(-25), RequestedByUserId = receptionUserId, Reason = "Rebalancing stock - high demand at Casuarina" },
            };

            context.Transfers.AddRange(transfers);

            context.SaveChanges();
        }
    }
}
