using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Queue.Migrations
{
    /// <inheritdoc />
    public partial class AddGroupIdInSubjectAndDate_Fix : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Subjects",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "GroupId",
                table: "Dates",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Subjects_GroupId",
                table: "Subjects",
                column: "GroupId");

            migrationBuilder.CreateIndex(
                name: "IX_Dates_GroupId",
                table: "Dates",
                column: "GroupId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Subjects_GroupId",
                table: "Subjects");

            migrationBuilder.DropIndex(
                name: "IX_Dates_GroupId",
                table: "Dates");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Subjects");

            migrationBuilder.DropColumn(
                name: "GroupId",
                table: "Dates");
        }
    }
}
