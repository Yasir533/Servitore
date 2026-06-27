using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servitore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddUpgradedServiceRegisterFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AgreementNumber",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "ApproximateCharges",
                table: "ServiceEntries",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CallType",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ComplaintMode",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustodyComponentsJson",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InvoiceNumber",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsChargeable",
                table: "ServiceEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "IsTomorrow",
                table: "ServiceEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "PendingForDocuments",
                table: "ServiceEntries",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "ServiceType",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "SubCallType",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TomorrowDays",
                table: "ServiceEntries",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AgreementNumber",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "ApproximateCharges",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "CallType",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "ComplaintMode",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "CustodyComponentsJson",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "InvoiceNumber",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "IsChargeable",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "IsTomorrow",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "PendingForDocuments",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "ServiceType",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "SubCallType",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "TomorrowDays",
                table: "ServiceEntries");
        }
    }
}
