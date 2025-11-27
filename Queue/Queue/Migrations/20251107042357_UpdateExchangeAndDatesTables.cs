using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Queue.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExchangeAndDatesTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "DateId",
                table: "Exchanges",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_Exchanges_DateId",
                table: "Exchanges",
                column: "DateId");

            migrationBuilder.AddForeignKey(
                name: "FK_Exchanges_Dates_DateId",
                table: "Exchanges",
                column: "DateId",
                principalTable: "Dates",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Exchanges_Dates_DateId",
                table: "Exchanges");

            migrationBuilder.DropIndex(
                name: "IX_Exchanges_DateId",
                table: "Exchanges");

            migrationBuilder.DropColumn(
                name: "DateId",
                table: "Exchanges");
        }
    }
}
