namespace LibrarySystem.Models.Dto
{
    /// <summary>A public branch listing. Exposes Code (the public identifier), Name, Address, Phone and OpeningHours; the internal numeric Id is left out, and the IsActive flag is implied (only active branches are ever returned) rather than echoed.</summary>
    public record BranchDto(string Code, string Name, string Address, string Phone, string OpeningHours);
}
