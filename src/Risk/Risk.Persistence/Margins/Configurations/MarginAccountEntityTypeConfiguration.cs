using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MarginAccountAggregate = Risk.Domain.Margins.MarginAccount;

namespace Risk.Persistence.Margins.Configurations;

public sealed class MarginAccountEntityTypeConfiguration : IEntityTypeConfiguration<MarginAccountAggregate>
{
    public void Configure(EntityTypeBuilder<MarginAccountAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("margin_accounts");

        builder.HasKey(marginAccount => marginAccount.Id);

        builder.Property(marginAccount => marginAccount.Id)
            .ValueGeneratedNever();

        builder.Property(marginAccount => marginAccount.AccountId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(marginAccount => marginAccount.CurrencyCode)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(marginAccount => marginAccount.TotalMargin)
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(marginAccount => marginAccount.ReservedMargin)
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(marginAccount => marginAccount.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(marginAccount => marginAccount.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(marginAccount => marginAccount.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(marginAccount => marginAccount.AccountId);
        builder.HasIndex(marginAccount => marginAccount.CurrencyCode);

        builder.Ignore(marginAccount => marginAccount.DomainEvents);
    }
}