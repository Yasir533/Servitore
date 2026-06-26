using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servitore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddMultiUserAuditFieldsPresenceLocks : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTickets_Users_CreatedBy",
                table: "ServiceTickets");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTickets_CreatedBy",
                table: "ServiceTickets");

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Warranties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Warranties",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Warranties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Warranties",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "Warranties",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "CreatedBy",
                table: "ServiceTickets",
                type: "nvarchar(max)",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "AssignedToUserId",
                table: "ServiceTickets",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CreatedByUserId",
                table: "ServiceTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "ServiceTickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "ServiceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Priority",
                table: "ServiceTickets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ResolutionNotes",
                table: "ServiceTickets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "SlaBreached",
                table: "ServiceTickets",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "SlaDueDate",
                table: "ServiceTickets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Customers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Customers",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "Assets",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PurchaseDate",
                table: "Assets",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Assets",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "VendorName",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CreatedBy",
                table: "AMCContracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedDate",
                table: "AMCContracts",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "ModifiedBy",
                table: "AMCContracts",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ModifiedDate",
                table: "AMCContracts",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "AMCContracts",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AMCVisits",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AMCContractId = table.Column<int>(type: "int", nullable: false),
                    ScheduledDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VisitDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    Status = table.Column<int>(type: "int", nullable: false),
                    Remarks = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EngineerId = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AMCVisits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AMCVisits_AMCContracts_AMCContractId",
                        column: x => x.AMCContractId,
                        principalTable: "AMCContracts",
                        principalColumn: "AMCContractId",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AMCVisits_Users_EngineerId",
                        column: x => x.EngineerId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AssetDocuments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AssetId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AssetDocuments_Assets_AssetId",
                        column: x => x.AssetId,
                        principalTable: "Assets",
                        principalColumn: "AssetId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTickets_AssignedToUserId",
                table: "ServiceTickets",
                column: "AssignedToUserId");

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTickets_CreatedByUserId",
                table: "ServiceTickets",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AMCVisits_AMCContractId",
                table: "AMCVisits",
                column: "AMCContractId");

            migrationBuilder.CreateIndex(
                name: "IX_AMCVisits_EngineerId",
                table: "AMCVisits",
                column: "EngineerId");

            migrationBuilder.CreateIndex(
                name: "IX_AssetDocuments_AssetId",
                table: "AssetDocuments",
                column: "AssetId");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTickets_Users_AssignedToUserId",
                table: "ServiceTickets",
                column: "AssignedToUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTickets_Users_CreatedByUserId",
                table: "ServiceTickets",
                column: "CreatedByUserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTickets_Users_AssignedToUserId",
                table: "ServiceTickets");

            migrationBuilder.DropForeignKey(
                name: "FK_ServiceTickets_Users_CreatedByUserId",
                table: "ServiceTickets");

            migrationBuilder.DropTable(
                name: "AMCVisits");

            migrationBuilder.DropTable(
                name: "AssetDocuments");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTickets_AssignedToUserId",
                table: "ServiceTickets");

            migrationBuilder.DropIndex(
                name: "IX_ServiceTickets_CreatedByUserId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "Warranties");

            migrationBuilder.DropColumn(
                name: "AssignedToUserId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "CreatedByUserId",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "Priority",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "ResolutionNotes",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "SlaBreached",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "SlaDueDate",
                table: "ServiceTickets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Customers");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "PurchaseDate",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "VendorName",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "AMCContracts");

            migrationBuilder.DropColumn(
                name: "CreatedDate",
                table: "AMCContracts");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "AMCContracts");

            migrationBuilder.DropColumn(
                name: "ModifiedDate",
                table: "AMCContracts");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "AMCContracts");

            migrationBuilder.AlterColumn<int>(
                name: "CreatedBy",
                table: "ServiceTickets",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_ServiceTickets_CreatedBy",
                table: "ServiceTickets",
                column: "CreatedBy");

            migrationBuilder.AddForeignKey(
                name: "FK_ServiceTickets_Users_CreatedBy",
                table: "ServiceTickets",
                column: "CreatedBy",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
