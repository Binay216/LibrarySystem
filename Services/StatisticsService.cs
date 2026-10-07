using LibrarySystem.Data;
using LibrarySystem.Models;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="IStatisticsService" />
    public class StatisticsService(ApplicationDbContext context, ILibraryClock clock) : IStatisticsService
    {
        /// <inheritdoc />
        public async Task<BorrowingStatsViewModel> GetBorrowingStatsAsync(int? branchId)
        {
            var today = clock.Today;

            var loans = context.Loans.AsQueryable();
            if (branchId is { } id)
            {
                loans = loans.Where(l => l.BorrowedAtBranchId == id);
            }

            var monthlyRaw = await loans
                .GroupBy(l => new { l.BorrowedDate.Year, l.BorrowedDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var loansOverTime = monthlyRaw
                .Select(g => new BorrowingStatsViewModel.MonthlyLoanCount(new DateTime(g.Year, g.Month, 1).ToString("MMM yyyy"), g.Count))
                .ToList();

            var mostBorrowed = await loans
                .GroupBy(l => l.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .Join(context.Items, g => g.ItemId, i => i.Id, (g, i) => new BorrowingStatsViewModel.TopBorrowedItem(i.Name, i.LibraryCode, g.Count))
                .ToListAsync();

            var activeLoans = await loans
                .Where(l => l.ReturnedDate == null)
                .OrderBy(l => l.DueDate)
                .Select(l => new BorrowingStatsViewModel.ActiveLoanSummary(
                    l.Item!.Name, l.Item.LibraryCode, l.Borrower!.FirstName + " " + l.Borrower.LastName, l.DueDate))
                .ToListAsync();

            var overdueRaw = await loans
                .Where(l => l.ReturnedDate == null && l.DueDate < today)
                .OrderBy(l => l.DueDate)
                .Select(l => new { ItemName = l.Item!.Name, l.Item.LibraryCode, BorrowerName = l.Borrower!.FirstName + " " + l.Borrower.LastName, l.DueDate })
                .ToListAsync();

            var overdueLoans = overdueRaw
                .Select(l => new BorrowingStatsViewModel.OverdueLoanSummary(l.ItemName, l.LibraryCode, l.BorrowerName, l.DueDate, (today - l.DueDate.Date).Days))
                .ToList();

            return new BorrowingStatsViewModel
            {
                LoansOverTime = loansOverTime,
                MostBorrowed = mostBorrowed,
                ActiveLoanCount = activeLoans.Count,
                ActiveLoans = activeLoans,
                OverdueLoanCount = overdueLoans.Count,
                OverdueLoans = overdueLoans
            };
        }

        /// <inheritdoc />
        public async Task<ItemStatusStatsViewModel> GetItemStatusStatsAsync(int? branchId)
        {
            var items = context.Items.AsQueryable();
            if (branchId is { } id)
            {
                items = items.Where(i => i.CurrentBranchId == id);
            }

            var overallCounts = await items.GroupBy(i => i.Status).ToDictionaryAsync(g => g.Key, g => g.Count());
            var statusCounts = Enum.GetValues<ItemStatus>()
                .Select(s => new ItemStatusStatsViewModel.StatusCount(s, overallCounts.GetValueOrDefault(s)))
                .ToList();

            var bookCounts = await GetCountsByStatusAsync<Book>(branchId);
            var musicCounts = await GetCountsByStatusAsync<Music>(branchId);
            var toyCounts = await GetCountsByStatusAsync<Toy>(branchId);

            var breakdownByType = new List<ItemStatusStatsViewModel.TypeStatusBreakdown>
            {
                BuildTypeBreakdown("Book", bookCounts),
                BuildTypeBreakdown("Music", musicCounts),
                BuildTypeBreakdown("Toy", toyCounts)
            };

            // One row per branch, each with its own status breakdown. Grouping by
            // (CurrentBranchId, Status) translates cleanly to SQL; the per-branch
            // dictionary lookup is done client-side since there are only a handful
            // of branches.
            var branchStatusRaw = await items
                .GroupBy(i => new { i.CurrentBranchId, i.Status })
                .Select(g => new { g.Key.CurrentBranchId, g.Key.Status, Count = g.Count() })
                .ToListAsync();
            var branchStatusLookup = branchStatusRaw.ToDictionary(r => (r.CurrentBranchId, r.Status), r => r.Count);

            var branchesQuery = context.Branches.AsQueryable();
            if (branchId is { } filterBranchId)
            {
                branchesQuery = branchesQuery.Where(b => b.Id == filterBranchId);
            }
            var branches = await branchesQuery.OrderBy(b => b.Name).ToListAsync();

            var breakdownByBranch = branches
                .Select(b => new ItemStatusStatsViewModel.BranchStatusBreakdown(
                    b.Name,
                    branchStatusLookup.GetValueOrDefault((b.Id, ItemStatus.Available)),
                    branchStatusLookup.GetValueOrDefault((b.Id, ItemStatus.Borrowed)),
                    branchStatusLookup.GetValueOrDefault((b.Id, ItemStatus.Damaged)),
                    branchStatusLookup.GetValueOrDefault((b.Id, ItemStatus.Destroyed))))
                .ToList();

            var awayFromHome = await items
                .Where(i => i.CurrentBranchId != i.HomeBranchId)
                .OrderBy(i => i.Name)
                .Select(i => new ItemStatusStatsViewModel.AwayFromHomeItem(i.Name, i.LibraryCode, i.HomeBranch!.Name, i.CurrentBranch!.Name))
                .ToListAsync();

            return new ItemStatusStatsViewModel
            {
                CountsByStatus = statusCounts,
                BreakdownByType = breakdownByType,
                BreakdownByBranch = breakdownByBranch,
                AwayFromHomeCount = awayFromHome.Count,
                AwayFromHomeItems = awayFromHome
            };
        }

        /// <inheritdoc />
        public async Task<FineStatsViewModel> GetFineStatsAsync(int? branchId)
        {
            var loans = context.Loans.Where(l => l.ReturnedDate != null);
            if (branchId is { } id)
            {
                loans = loans.Where(l => l.BorrowedAtBranchId == id);
            }

            var returnedLoans = await loans
                .Select(l => new { l.FineAmount, l.FinePaid })
                .ToListAsync();

            var totalAccrued = returnedLoans.Sum(l => l.FineAmount);
            var totalPaid = returnedLoans.Where(l => l.FinePaid).Sum(l => l.FineAmount);
            var totalOutstanding = returnedLoans.Where(l => !l.FinePaid).Sum(l => l.FineAmount);
            var averageFine = returnedLoans.Count > 0 ? Math.Round(totalAccrued / returnedLoans.Count, 2) : 0m;

            return new FineStatsViewModel
            {
                TotalAccrued = totalAccrued,
                TotalPaid = totalPaid,
                TotalOutstanding = totalOutstanding,
                AverageFinePerReturnedLoan = averageFine,
                ReturnedLoanCount = returnedLoans.Count,
                FinedLoanCount = returnedLoans.Count(l => l.FineAmount > 0)
            };
        }

        /// <inheritdoc />
        public async Task<List<ItemListItemViewModel>> GetFeaturedItemsAsync()
        {
            var featuredBook = await context.Items.OfType<Book>().AsNoTracking().Include(i => i.CurrentBranch).Where(i => i.Status == ItemStatus.Available).OrderBy(i => i.Name).FirstOrDefaultAsync();
            var featuredMusic = await context.Items.OfType<Music>().AsNoTracking().Include(i => i.CurrentBranch).Where(i => i.Status == ItemStatus.Available).OrderBy(i => i.Name).FirstOrDefaultAsync();
            var featuredToy = await context.Items.OfType<Toy>().AsNoTracking().Include(i => i.CurrentBranch).Where(i => i.Status == ItemStatus.Available).OrderBy(i => i.Name).FirstOrDefaultAsync();

            return new Item?[] { featuredBook, featuredMusic, featuredToy }
                .Where(i => i is not null)
                .Select(i => ItemListItemViewModel.FromItem(i!))
                .ToList();
        }

        private async Task<Dictionary<ItemStatus, int>> GetCountsByStatusAsync<TItem>(int? branchId) where TItem : Item
        {
            var query = context.Items.OfType<TItem>().AsQueryable();
            if (branchId is { } id)
            {
                query = query.Where(i => i.CurrentBranchId == id);
            }

            return await query.GroupBy(i => i.Status).ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private static ItemStatusStatsViewModel.TypeStatusBreakdown BuildTypeBreakdown(string typeLabel, Dictionary<ItemStatus, int> counts) => new(
            typeLabel,
            counts.GetValueOrDefault(ItemStatus.Available),
            counts.GetValueOrDefault(ItemStatus.Borrowed),
            counts.GetValueOrDefault(ItemStatus.Damaged),
            counts.GetValueOrDefault(ItemStatus.Destroyed));
    }
}
