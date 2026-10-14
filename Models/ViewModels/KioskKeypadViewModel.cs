namespace LibrarySystem.Models.ViewModels
{
    /// <summary>Configures the reusable on-screen keypad partial for one input field.</summary>
    public class KioskKeypadViewModel
    {
        /// <summary>CSS selector of the input this keypad types into, e.g. "#libraryCode".</summary>
        public required string TargetSelector { get; init; }

        public required KioskKeypadMode Mode { get; init; }
    }

    public enum KioskKeypadMode
    {
        Numeric,
        Alphanumeric
    }
}
