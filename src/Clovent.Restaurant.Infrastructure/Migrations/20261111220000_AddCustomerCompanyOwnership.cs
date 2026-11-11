using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Clovent.Restaurant.Infrastructure.Persistence;

#nullable disable
namespace Clovent.Restaurant.Infrastructure.Migrations;

/// <summary>Expands ownership metadata without assigning historical owners.</summary>
[DbContext(typeof(RestaurantDbContext))]
[Migration("20261111220000_AddCustomerCompanyOwnership")]
public partial class AddCustomerCompanyOwnership : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Expand only. Never infer or assign a historical company in an automatic migration.
        migrationBuilder.AddColumn<Guid>("CompanyId", "Customers", "uniqueidentifier", schema: "Restaurant", nullable: true);
        migrationBuilder.AddColumn<Guid>("CompanyId", "CustomerLedgerEntries", "uniqueidentifier", schema: "Restaurant", nullable: true);
        migrationBuilder.CreateIndex("IX_Customers_CompanyId_Id", "Customers", new[] { "CompanyId", "Id" }, "Restaurant", unique: true);
        migrationBuilder.CreateIndex("IX_CustomerLedgerEntries_CompanyId_CustomerId", "CustomerLedgerEntries",
            new[] { "CompanyId", "CustomerId" }, "Restaurant");
        migrationBuilder.AddForeignKey(name: "FK_CustomerLedger_CompanyCustomer", table: "CustomerLedgerEntries",
            columns: new[] { "CompanyId", "CustomerId" }, schema: "Restaurant", principalTable: "Customers", principalSchema: "Restaurant",
            principalColumns: new[] { "CompanyId", "Id" }, onDelete: ReferentialAction.NoAction);
        migrationBuilder.Sql("""
            CREATE TABLE [Restaurant].[CustomerCompanyOwnershipMap] (
              [CustomerId] uniqueidentifier NOT NULL PRIMARY KEY,
              [CompanyId] uniqueidentifier NOT NULL,
              [ApprovedBy] nvarchar(200) NOT NULL,
              [ApprovedAtUtc] datetimeoffset NOT NULL,
              [EvidenceReference] nvarchar(500) NOT NULL
            );
            """);
    }

    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Ownership rollback would remove audit metadata. Use a reviewed forward repair.");
}
