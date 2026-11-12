using Clovent.Restaurant.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Clovent.Restaurant.Infrastructure.Migrations;

/// <summary>Protects established ownership even when a bulk update bypasses SaveChanges.</summary>
[DbContext(typeof(RestaurantDbContext))]
[Migration("20261112010000_PreventCompanyOwnershipReassignment")]
public sealed partial class PreventCompanyOwnershipReassignment : Migration
{
    /// <inheritdoc/>
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        foreach (var table in new[] { "Customers", "CustomerLedgerEntries" })
        {
            migrationBuilder.Sql($"""
                CREATE TRIGGER [Restaurant].[TR_{table}_CompanyImmutable] ON [Restaurant].[{table}]
                AFTER UPDATE AS
                BEGIN
                    SET NOCOUNT ON;
                    IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.Id=d.Id
                        WHERE d.CompanyId IS NOT NULL AND (i.CompanyId IS NULL OR i.CompanyId<>d.CompanyId))
                        THROW 51012, 'Established company ownership cannot be reassigned by bulk or direct updates.', 1;
                END;
                """);
            migrationBuilder.Sql($"""
                CREATE TRIGGER [Restaurant].[TR_{table}_NoDelete] ON [Restaurant].[{table}]
                INSTEAD OF DELETE AS
                    THROW 51013, 'Customer and ledger history cannot be physically deleted.', 1;
                """);
        }
        migrationBuilder.Sql("""
            DENY SELECT, INSERT, UPDATE, DELETE ON OBJECT::[Restaurant].[CustomerCompanyOwnershipMap] TO [public];
            """);
        migrationBuilder.Sql("""
            CREATE TRIGGER [Restaurant].[TR_Customers_RequireApprovedLegacyOwner] ON [Restaurant].[Customers]
            AFTER UPDATE AS
            BEGIN
                SET NOCOUNT ON;
                IF EXISTS (SELECT 1 FROM inserted i JOIN deleted d ON i.Id=d.Id
                    WHERE d.CompanyId IS NULL AND i.CompanyId IS NOT NULL AND NOT EXISTS
                      (SELECT 1 FROM Restaurant.CustomerCompanyOwnershipMap m
                       WHERE m.CustomerId=i.Id AND m.CompanyId=i.CompanyId
                         AND LEN(LTRIM(RTRIM(m.ApprovedBy)))>0 AND LEN(LTRIM(RTRIM(m.EvidenceReference)))>0))
                    THROW 51015, 'Legacy ownership requires an explicitly approved mapping.', 1;
            END;
            """);
    }
    /// <inheritdoc/>
    protected override void Down(MigrationBuilder migrationBuilder)
        => throw new NotSupportedException("Removing ownership protection requires a reviewed forward repair.");
}
