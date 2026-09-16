namespace LibrarySystem.Services
{
    /// <summary>
    /// Fine policy values, bound from the "FinePolicy" section of appsettings.json.
    /// Never hardcode these elsewhere - <see cref="LoanService.CalculateFine"/> is the
    /// one place they are applied.
    /// </summary>
    public class FinePolicyOptions
    {
        public const string SectionName = "FinePolicy";

        public int LoanPeriodDays { get; set; } = 14;

        public decimal DailyLateFee { get; set; } = 0.50m;

        public decimal MaximumFine { get; set; } = 20.00m;

        public decimal DamagedSurcharge { get; set; } = 10.00m;

        public decimal DestroyedReplacementFee { get; set; } = 50.00m;
    }
}
