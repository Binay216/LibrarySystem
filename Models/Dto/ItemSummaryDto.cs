namespace LibrarySystem.Models.Dto
{
    /// <summary>
    /// One item in the public catalogue listing. A dedicated shape, never the EF
    /// Item entity - serializing an entity directly risks pulling its Loans
    /// navigation collection (and transitively, Borrower names/emails/phones)
    /// out over the wire by accident. This type can't do that: it has no
    /// loan or borrower fields at all, by construction.
    ///
    /// Exposed: LibraryCode (the public identifier - not the internal numeric
    /// Id, which would leak row count/insertion order to anyone watching it
    /// increment), Name, Description, Type, Status, a flattened Summary
    /// (author/genre etc. - same flattening ItemListItemViewModel already does
    /// for the MVC catalog), CurrentBranchName (where to go for it) and
    /// QueueCount (people ahead in the reservation queue).
    ///
    /// Deliberately left out: HomeBranchName/"away from home" - internal
    /// branch-logistics bookkeeping, not something a public API consumer needs;
    /// and anything reservation-identity-level (who's waiting) - only the count
    /// is exposed, never names.
    /// </summary>
    public record ItemSummaryDto(
        string LibraryCode,
        string Name,
        string Description,
        string Type,
        string Status,
        string Summary,
        string CurrentBranchName,
        int QueueCount);
}
