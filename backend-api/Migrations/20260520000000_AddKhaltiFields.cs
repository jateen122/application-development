using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddKhaltiFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "KhaltiPidx",
                table: "SaleInvoices",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "KhaltiTransactionId",
                table: "SaleInvoices",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "KhaltiPidx",          table: "SaleInvoices");
            migrationBuilder.DropColumn(name: "KhaltiTransactionId", table: "SaleInvoices");
        }
    }
}
