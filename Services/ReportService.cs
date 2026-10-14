using LibrarySystem.Data;
using LibrarySystem.Models.Enums;
using LibrarySystem.Models.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Services
{
    /// <inheritdoc cref="IReportService" />
    public class ReportService(ApplicationDbContext context, ILibraryClock clock, IStatisticsService statisticsService) : IReportService
    {
        /// <inheritdoc />
        public async Task<BorrowingReportViewModel> BorrowingReportAsync(DateTime startDate, DateTime endDate, int? branchId)
        {
            var today = clock.Today;

            var loans = context.Loans
                .Where(l => l.BorrowedDate.Date >= startDate.Date && l.BorrowedDate.Date <= endDate.Date);

            if (branchId is { } id)
            {
                loans = loans.Where(l => l.BorrowedAtBranchId == id);
            }

            var totalLoans = await loans.CountAsync();
            var activeLoanCount = await loans.CountAsync(l => l.ReturnedDate == null);
            var overdueCount = await loans.CountAsync(l => l.ReturnedDate == null && l.DueDate.Date < today);

            var mostBorrowed = await loans
                .GroupBy(l => l.ItemId)
                .Select(g => new { ItemId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Join(context.Items, g => g.ItemId, i => i.Id, (g, i) =>
                    new BorrowingReportViewModel.TopBorrowedItem(i.Name, i.LibraryCode, g.Count))
                .ToListAsync();

            var branchBreakdown = await loans
                .GroupBy(l => l.BorrowedAtBranchId)
                .Select(g => new { BranchId = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .Join(context.Branches, g => g.BranchId, b => b.Id, (g, b) =>
                    new BorrowingReportViewModel.BranchLoanBreakdown(b.Name, g.Count))
                .ToListAsync();

            return new BorrowingReportViewModel
            {
                Filters = await BuildFilterInfoAsync(startDate, endDate, branchId),
                TotalLoans = totalLoans,
                ActiveLoanCount = activeLoanCount,
                OverdueCount = overdueCount,
                MostBorrowed = mostBorrowed,
                BreakdownByBranch = branchBreakdown
            };
        }

        /// <inheritdoc />
        public async Task<FineRevenueReportViewModel> FineRevenueReportAsync(DateTime startDate, DateTime endDate, int? branchId)
        {
            var loansQuery = context.Loans
                .Include(l => l.Borrower)
                .Where(l => l.ReturnedDate != null
                    && l.ReturnedDate.Value.Date >= startDate.Date
                    && l.ReturnedDate.Value.Date <= endDate.Date
                    && l.FineAmount > 0);

            if (branchId is { } id)
            {
                loansQuery = loansQuery.Where(l => l.BorrowedAtBranchId == id);
            }

            // Grouped client-side rather than via further EF queries: a report's
            // fine-bearing returns for one period are a small set, and both
            // groupings (by borrower, by month) are needed from the same rows.
            var loans = await loansQuery.ToListAsync();

            var totalAccrued = loans.Sum(l => l.FineAmount);
            var totalPaid = loans.Where(l => l.FinePaid).Sum(l => l.FineAmount);
            var totalOutstanding = loans.Where(l => !l.FinePaid).Sum(l => l.FineAmount);

            var byBorrower = loans
                .GroupBy(l => l.BorrowerId)
                .Select(g => new FineRevenueReportViewModel.BorrowerFineBreakdown(
                    $"{g.First().Borrower!.FirstName} {g.First().Borrower!.LastName}",
                    g.Sum(l => l.FineAmount),
                    g.Where(l => l.FinePaid).Sum(l => l.FineAmount),
                    g.Where(l => !l.FinePaid).Sum(l => l.FineAmount)))
                .OrderByDescending(b => b.Accrued)
                .ToList();

            var byPeriod = loans
                .GroupBy(l => new { l.ReturnedDate!.Value.Year, l.ReturnedDate.Value.Month })
                .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
                .Select(g => new FineRevenueReportViewModel.PeriodFineBreakdown(
                    new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                    g.Sum(l => l.FineAmount),
                    g.Where(l => l.FinePaid).Sum(l => l.FineAmount),
                    g.Where(l => !l.FinePaid).Sum(l => l.FineAmount)))
                .ToList();

            return new FineRevenueReportViewModel
            {
                Filters = await BuildFilterInfoAsync(startDate, endDate, branchId),
                TotalAccrued = totalAccrued,
                TotalPaid = totalPaid,
                TotalOutstanding = totalOutstanding,
                ByBorrower = byBorrower,
                ByPeriod = byPeriod
            };
        }

        /// <inheritdoc />
        public async Task<InventoryHealthReportViewModel> InventoryHealthReportAsync(DateTime startDate, DateTime endDate, int? branchId)
        {
            var itemStatusStats = await statisticsService.GetItemStatusStatsAsync(branchId);

            var itemsQuery = context.Items.AsNoTracking().Include(i => i.CurrentBranch).AsQueryable();
            if (branchId is { } id)
            {
                itemsQuery = itemsQuery.Where(i => i.CurrentBranchId == id);
            }

            var damagedItems = await itemsQuery
                .Where(i => i.Status == ItemStatus.Damaged)
                .OrderBy(i => i.Name)
                .Select(i => new InventoryHealthReportViewModel.InventoryItemSummary(i.Name, i.LibraryCode, i.CurrentBranch!.Name))
                .ToListAsync();

            var destroyedItems = await itemsQuery
                .Where(i => i.Status == ItemStatus.Destroyed)
                .OrderBy(i => i.Name)
                .Select(i => new InventoryHealthReportViewModel.InventoryItemSummary(i.Name, i.LibraryCode, i.CurrentBranch!.Name))
                .ToListAsync();

            return new InventoryHealthReportViewModel
            {
                Filters = await BuildFilterInfoAsync(startDate, endDate, branchId),
                CountsByStatus = itemStatusStats.CountsByStatus,
                BreakdownByType = itemStatusStats.BreakdownByType,
                BreakdownByBranch = itemStatusStats.BreakdownByBranch,
                AwayFromHomeCount = itemStatusStats.AwayFromHomeCount,
                AwayFromHomeItems = itemStatusStats.AwayFromHomeItems,
                DamagedItems = damagedItems,
                DestroyedItems = destroyedItems
            };
        }

        private async Task<ReportFilterInfo> BuildFilterInfoAsync(DateTime startDate, DateTime endDate, int? branchId)
        {
            var branchName = "All Branches";
            if (branchId is { } id)
            {
                var branch = await context.Branches.FindAsync(id);
                branchName = branch?.Name ?? "Unknown Branch";
            }

            return new ReportFilterInfo { StartDate = startDate, EndDate = endDate, BranchName = branchName };
        }
    }
}
