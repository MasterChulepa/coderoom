using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CodeRoom.Api.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateIdAndIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollaborativeDocumentUpdates_FileId",
                table: "CollaborativeDocumentUpdates");

            migrationBuilder.AddColumn<Guid>(
                name: "UpdateId",
                table: "CollaborativeDocumentUpdates",
                type: "uuid",
                nullable: true);

            migrationBuilder.Sql("""
                UPDATE "CollaborativeDocumentUpdates"
                SET "UpdateId" = gen_random_uuid()
                WHERE "UpdateId" IS NULL;
            """);

            migrationBuilder.AlterColumn<Guid>(
                name: "UpdateId",
                table: "CollaborativeDocumentUpdates",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: true);


            migrationBuilder.CreateIndex(
                name: "IX_CollaborativeDocumentUpdates_FileId_UpdateId",
                table: "CollaborativeDocumentUpdates",
                columns: new[] { "FileId", "UpdateId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CollaborativeDocumentUpdates_FileId_UpdateId",
                table: "CollaborativeDocumentUpdates");

            migrationBuilder.DropColumn(
                name: "UpdateId",
                table: "CollaborativeDocumentUpdates");

            migrationBuilder.CreateIndex(
                name: "IX_CollaborativeDocumentUpdates_FileId",
                table: "CollaborativeDocumentUpdates",
                column: "FileId");
        }
    }
}
