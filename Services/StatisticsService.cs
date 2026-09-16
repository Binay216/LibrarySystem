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
        public async Task<BorrowingStatsViewModel> GetBorrowingStatsAsync()
        {
            var today = clock.Today;

            var monthlyRaw = await context.Loans
                .GroupBy(l => new { l.BorrowedDate.Year, l.BorrowedDate.Month })
                .Select(g => new { g.Key.Year, g.Key.Month, Count = g.Count() })
                .OrderBy(g => g.Year).ThenBy(g => g.Month)
                .ToListAsync();

            var loansOverTime = monthlyRaw
                .Select(g => new BorrowingStatsViewModel.MonthlyLoanCount(new DateTime(g.Year, g.Month, 1).ToString("MMM yyyy"), g.Count))
                .ToList();

            var mostBorrowed = await context.Loans
                .GroupBy(l => l.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Take(5)
                .Join(context.Items, g => g.ItemId, i => i.Id, (g, i) => new BorrowingStatsViewModel.TopBorrowedItem(i.Name, i.LibraryCode, g.Count))
                .ToListAsync();

            var activeLoans = await context.Loans
                .Where(l => l.ReturnedDate == null)
                .OrderBy(l => l.DueDate)
                .Select(l => new BorrowingStatsViewModel.ActiveLoanSummary(
                    l.Item!.Name, l.Item.LibraryCode, l.Borrower!.FirstName + " " + l.Borrower.LastName, l.DueDate))
                .ToListAsync();

            var overdueRaw = await context.Loans
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
        public async Task<ItemStatusStatsViewModel> GetItemStatusStatsAsync()
        {
            var overallCounts = await context.Items.GroupBy(i => i.Status).ToDictionaryAsync(g => g.Key, g => g.Count());
            var statusCounts = Enum.GetValues<ItemStatus>()
                .Select(s => new ItemStatusStatsViewModel.StatusCount(s, overallCounts.GetValueOrDefault(s)))
                .ToList();

            var bookCounts = await GetCountsByStatusAsync<Book>();
            var musicCounts = await GetCountsByStatusAsync<Music>();
            var toyCounts = await GetCountsByStatusAsync<Toy>();

            var breakdown = new List<ItemStatusStatsViewModel.TypeStatusBreakdown>
            {
                BuildBreakdown("Book", bookCounts),
                BuildBreakdown("Music", musicCounts),
                BuildBreakdown("Toy", toyCounts)
            };

            return new ItemStatusStatsViewModel { CountsByStatus = statusCounts, BreakdownByType = breakdown };
        }

        /// <inheritdoc />
        public async Task<FineStatsViewModel> GetFineStatsAsync()
        {
            var returnedLoans = await context.Loans
                .Where(l => l.ReturnedDate != null)
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
            var featuredBook = await context.Items.OfType<Book>().AsNoTracking().Where(i => i.Status == ItemStatus.Available).OrderBy(i => i.Name).FirstOrDefaultAsync();
            var featuredMusic = await context.Items.OfType<Music>().AsNoTracking().Where(i => i.Status == ItemStatus.Available).OrderBy(i => i.Name).FirstOrDefaultAsync();
            var featuredToy = await context.Items.OfType<Toy>().AsNoTracking().Where(i => i.Status == ItemStatus.Available).OrderBy(i => i.Name).FirstOrDefaultAsync();

            return new Item?[] { featuredBook, featuredMusic, featuredToy }
                .Where(i => i is not null)
                .Select(i => ItemListItemViewModel.FromItem(i!))
                .ToList();
        }

        private async Task<Dictionary<ItemStatus, int>> GetCountsByStatusAsync<TItem>() where TItem : Item
        {
            return await context.Items.OfType<TItem>().GroupBy(i => i.Status).ToDictionaryAsync(g => g.Key, g => g.Count());
        }

        private static ItemStatusStatsViewModel.TypeStatusBreakdown BuildBreakdown(string typeLabel, Dictionary<ItemStatus, int> counts) => new(
            typeLabel,
            counts.GetValueOrDefault(ItemStatus.Available),
            counts.GetValueOrDefault(ItemStatus.Borrowed),
            counts.GetValueOrDefault(ItemStatus.Damaged),
            counts.GetValueOrDefault(ItemStatus.Destroyed));
    }
}
