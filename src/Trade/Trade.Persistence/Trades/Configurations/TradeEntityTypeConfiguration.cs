using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TradeAggregate = Trade.Domain.Trades.Trade;

namespace Trade.Persistence.Trades.Configurations;

public sealed class TradeEntityTypeConfiguration : IEntityTypeConfiguration<TradeAggregate>
{
    public void Configure(EntityTypeBuilder<TradeAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("trades");

        builder.HasKey(trade => trade.Id);

        builder.Property(trade => trade.Id)
            .ValueGeneratedNever();

        builder.Property(trade => trade.AccountId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(trade => trade.InstrumentId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(trade => trade.Direction)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(trade => trade.Status)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(trade => trade.Quantity)
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(trade => trade.OpenPrice)
            .HasColumnType("numeric(18,10)")
            .IsRequired();

        builder.Property(trade => trade.OpenedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(trade => trade.ClosePrice)
            .HasColumnType("numeric(18,10)");

        builder.Property(trade => trade.ClosedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(trade => trade.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(trade => trade.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(trade => trade.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(trade => trade.AccountId);
        builder.HasIndex(trade => trade.InstrumentId);
        builder.HasIndex(trade => trade.Status);

        builder.Ignore(trade => trade.DomainEvents);
    }
}