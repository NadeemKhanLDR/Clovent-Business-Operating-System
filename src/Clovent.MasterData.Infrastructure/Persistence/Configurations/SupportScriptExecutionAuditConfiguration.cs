using Clovent.MasterData.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Clovent.MasterData.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework Core mapping configuration for <see cref="SupportScriptExecutionAudit"/>.
/// Maps to table [MasterData].[SupportScriptExecutionAudit].
/// </summary>
public sealed class SupportScriptExecutionAuditConfiguration : IEntityTypeConfiguration<SupportScriptExecutionAudit>
{
    /// <inheritdoc/>
    public void Configure(EntityTypeBuilder<SupportScriptExecutionAudit> builder)
    {
        builder.ToTable("SupportScriptExecutionAudit", "MasterData");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .ValueGeneratedOnAdd();

        builder.Property(a => a.ScriptId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ScriptHash)
            .HasMaxLength(64)
            .IsUnicode(false)
            .IsRequired();

        builder.Property(a => a.Operator)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.ExecutedAtUtc)
            .HasColumnType("datetime2")
            .IsRequired();

        builder.Property(a => a.TargetDatabase)
            .HasMaxLength(128)
            .IsRequired();

        builder.Property(a => a.PreCheckSummary)
            .IsRequired(false);

        builder.Property(a => a.AffectedRows)
            .IsRequired();

        builder.Property(a => a.Outcome)
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(a => a.ErrorMessage)
            .IsRequired(false);

        builder.Property(a => a.CompensationReference)
            .HasMaxLength(200)
            .IsRequired(false);

        builder.HasIndex(a => a.ScriptId)
            .HasDatabaseName("IX_SupportScriptExecutionAudit_ScriptId");

        builder.HasIndex(a => a.ScriptId)
            .HasDatabaseName("UX_SupportScriptExecutionAudit_ScriptId_Success")
            .IsUnique()
            .HasFilter("[Outcome] = 'Success'");
    }
}
