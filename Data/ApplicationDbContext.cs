using LibrarySystem.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace LibrarySystem.Data
{
    /// <summary>EF Core Identity plus the library's own domain model - items, borrowers and loans.</summary>
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext(options)
    {
        public DbSet<Item> Items => Set<Item>();

        public DbSet<Borrower> Borrowers => Set<Borrower>();

        public DbSet<Loan> Loans => Set<Loan>();

        public DbSet<Branch> Branches => Set<Branch>();

        public DbSet<Transfer> Transfers => Set<Transfer>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Table-per-hierarchy: one Items table with a discriminator column, so the
            // public search portal and admin item list can query all types with no joins.
            modelBuilder.Entity<Item>()
                .HasDiscriminator<string>("ItemType")
                .HasValue<Book>("Book")
                .HasValue<Music>("Music")
                .HasValue<Toy>("Toy");

            modelBuilder.Entity<Item>()
                .HasIndex(i => i.LibraryCode)
                .IsUnique();

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Item)
                .WithMany(i => i.Loans)
                .HasForeignKey(l => l.ItemId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.Borrower)
                .WithMany(b => b.Loans)
                .HasForeignKey(l => l.BorrowerId)
                .OnDelete(DeleteBehavior.Restrict);

            // Branches. Restrict everywhere a branch is referenced, same reasoning as
            // the Loan FKs above - a branch with items, loans or transfers against it
            // can't be deleted out from under them.
            modelBuilder.Entity<Branch>()
                .HasIndex(b => b.Code)
                .IsUnique();

            // Item has two FKs to Branch (home vs current), so each needs its own
            // explicit HasOne/WithMany - EF can't infer which is which on its own.
            modelBuilder.Entity<Item>()
                .HasOne(i => i.HomeBranch)
                .WithMany()
                .HasForeignKey(i => i.HomeBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Item>()
                .HasOne(i => i.CurrentBranch)
                .WithMany()
                .HasForeignKey(i => i.CurrentBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.BorrowedAtBranch)
                .WithMany()
                .HasForeignKey(l => l.BorrowedAtBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Loan>()
                .HasOne(l => l.ReturnedAtBranch)
                .WithMany()
                .HasForeignKey(l => l.ReturnedAtBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            // Cascade, unlike the branch FKs above: an item's transfer history isn't
            // a reason to block deleting the item itself (admin item delete already
            // has its own loan-history guard for that), so a deleted item should take
            // its transfer requests with it rather than block the delete.
            modelBuilder.Entity<Transfer>()
                .HasOne(t => t.Item)
                .WithMany()
                .HasForeignKey(t => t.ItemId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<Transfer>()
                .HasOne(t => t.FromBranch)
                .WithMany()
                .HasForeignKey(t => t.FromBranchId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Transfer>()
                .HasOne(t => t.ToBranch)
                .WithMany()
                .HasForeignKey(t => t.ToBranchId)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
