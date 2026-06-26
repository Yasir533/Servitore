using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servitore.Database.Migrations
{
    /// <inheritdoc />
    public partial class RenameServiceTicketToServiceEntry : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // 1. Rename Column ContactPerson to Company on Customers
            migrationBuilder.RenameColumn(
                name: "ContactPerson",
                table: "Customers",
                newName: "Company");

            // 2. Add Column Notes on Customers
            migrationBuilder.AddColumn<string>(
                name: "Notes",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            // 3. Rename ServiceTickets table to ServiceEntries
            migrationBuilder.RenameTable(
                name: "ServiceTickets",
                newName: "ServiceEntries");

            // 4. Rename columns on ServiceEntries
            migrationBuilder.RenameColumn(
                table: "ServiceEntries",
                name: "TicketId",
                newName: "ServiceEntryId");

            migrationBuilder.RenameColumn(
                table: "ServiceEntries",
                name: "TicketNumber",
                newName: "ServiceEntryNumber");

            // 5. Rename TicketHistories table to ServiceEntryHistories
            migrationBuilder.RenameTable(
                name: "TicketHistories",
                newName: "ServiceEntryHistories");

            // 6. Rename columns on ServiceEntryHistories
            migrationBuilder.RenameColumn(
                table: "ServiceEntryHistories",
                name: "TicketId",
                newName: "ServiceEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                table: "ServiceEntryHistories",
                name: "ServiceEntryId",
                newName: "TicketId");

            migrationBuilder.RenameTable(
                name: "ServiceEntryHistories",
                newName: "TicketHistories");

            migrationBuilder.RenameColumn(
                table: "ServiceEntries",
                name: "ServiceEntryNumber",
                newName: "TicketNumber");

            migrationBuilder.RenameColumn(
                table: "ServiceEntries",
                name: "ServiceEntryId",
                newName: "TicketId");

            migrationBuilder.RenameTable(
                name: "ServiceEntries",
                newName: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "Notes",
                table: "Customers");

            migrationBuilder.RenameColumn(
                table: "Customers",
                name: "Company",
                newName: "ContactPerson");
        }
    }
}
