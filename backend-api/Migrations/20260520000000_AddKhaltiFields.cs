using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WeatherAPI.Migrations
{
    /// <summary>
    /// Adds KhaltiPidx and KhaltiTransactionId to SaleInvoices.
    /// Uses IF NOT EXISTS so it is safe to run even if columns already exist.
    /// </summary>
    public partial class AddKhaltiFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Use raw SQL with IF NOT EXISTS to avoid errors if already applied manually
            migrationBuilder.Sql(@"
                DO $$
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='SaleInvoices' AND column_name='KhaltiPidx'
                    ) THEN
                        ALTER TABLE ""SaleInvoices"" ADD COLUMN ""KhaltiPidx"" text NULL;
                    END IF;

                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns
                        WHERE table_name='SaleInvoices' AND column_name='KhaltiTransactionId'
                    ) THEN
                        ALTER TABLE ""SaleInvoices"" ADD COLUMN ""KhaltiTransactionId"" text NULL;
                    END IF;
                END
                $$;
            ");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(@"
                ALTER TABLE ""SaleInvoices""
                    DROP COLUMN IF EXISTS ""KhaltiPidx"",
                    DROP COLUMN IF EXISTS ""KhaltiTransactionId"";
            ");
        }
    }
}
