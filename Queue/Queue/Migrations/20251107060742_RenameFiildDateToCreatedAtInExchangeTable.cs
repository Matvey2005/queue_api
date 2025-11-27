using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Queue.Migrations
{
    /// <inheritdoc />
    public partial class RenameFiildDateToCreatedAtInExchangeTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "Date",
                table: "Exchanges",
                newName: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CreatedAt",
                table: "Exchanges",
                newName: "Date");
        }
    }
}
