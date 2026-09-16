namespace LibrarySystem.Models.Enums
{
    /// <summary>
    /// The lifecycle state of a library item: available -> borrowed -> returned
    /// (back to available), with damaged and destroyed as off-ramps.
    /// </summary>
    public enum ItemStatus
    {
        Available,
        Borrowed,
        Damaged,
        Destroyed
    }
}
