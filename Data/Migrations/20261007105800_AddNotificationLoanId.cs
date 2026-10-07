using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LibrarySystem.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddNotificationLoanId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LoanId",
                table: "Notifications",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_LoanId",
                table: "Notifications",
                column: "LoanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Notifications_Loans_LoanId",
                table: "Notifications",
                column: "LoanId",
                principalTable: "Loans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Notifications_Loans_LoanId",
                table: "Notifications");

            migrationBuilder.DropIndex(
                name: "IX_Notifications_LoanId",
                table: "Notifications");

            migrationBuilder.DropColumn(
                name: "LoanId",
                table: "Notifications");
        }
    }
}
