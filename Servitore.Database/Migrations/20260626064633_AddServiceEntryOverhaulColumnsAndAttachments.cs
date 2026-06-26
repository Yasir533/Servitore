using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Servitore.Database.Migrations
{
    /// <inheritdoc />
    public partial class AddServiceEntryOverhaulColumnsAndAttachments : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AccessoriesReceived",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Solution",
                table: "ServiceEntries",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Brand",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Model",
                table: "Assets",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ServiceEntryAttachments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ServiceEntryId = table.Column<int>(type: "int", nullable: false),
                    FileName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FilePath = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AttachmentType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    UploadedDate = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ServiceEntryAttachments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ServiceEntryAttachments_ServiceEntries_ServiceEntryId",
                        column: x => x.ServiceEntryId,
                        principalTable: "ServiceEntries",
                        principalColumn: "ServiceEntryId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ServiceEntryAttachments_ServiceEntryId",
                table: "ServiceEntryAttachments",
                column: "ServiceEntryId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ServiceEntryAttachments");

            migrationBuilder.DropColumn(
                name: "AccessoriesReceived",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "Solution",
                table: "ServiceEntries");

            migrationBuilder.DropColumn(
                name: "Brand",
                table: "Assets");

            migrationBuilder.DropColumn(
                name: "Model",
                table: "Assets");
        }
    }
}
