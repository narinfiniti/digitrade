using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SettlementAggregate = Settlement.Domain.Settlements.Settlement;

namespace Settlement.Persistence.Settlements.Configurations;

public sealed class SettlementEntityTypeConfiguration : IEntityTypeConfiguration<SettlementAggregate>
{
    public void Configure(EntityTypeBuilder<SettlementAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("settlements");

        builder.HasKey(settlement => settlement.Id);

        builder.Property(settlement => settlement.Id)
            .ValueGeneratedNever();

        builder.Property(settlement => settlement.TradeId)
            .IsRequired();

        builder.Property(settlement => settlement.AccountId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(settlement => settlement.CurrencyCode)
            .HasMaxLength(8)
            .IsRequired();

        builder.Property(settlement => settlement.NetAmount)
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(settlement => settlement.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(settlement => settlement.InitiatedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(settlement => settlement.FinalizedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(settlement => settlement.FailedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(settlement => settlement.FailureReason)
            .HasMaxLength(512);

        builder.Property(settlement => settlement.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(settlement => settlement.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(settlement => settlement.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(settlement => settlement.TradeId);
        builder.HasIndex(settlement => settlement.AccountId);
        builder.HasIndex(settlement => settlement.Status);

        builder.Ignore(settlement => settlement.DomainEvents);
    }
}