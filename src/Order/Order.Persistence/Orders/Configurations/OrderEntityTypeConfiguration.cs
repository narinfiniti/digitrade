using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderAggregate = Order.Domain.Orders.Order;

namespace Order.Persistence.Orders.Configurations;

public sealed class OrderEntityTypeConfiguration : IEntityTypeConfiguration<OrderAggregate>
{
    public void Configure(EntityTypeBuilder<OrderAggregate> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("orders");

        builder.HasKey(order => order.Id);

        builder.Property(order => order.Id)
            .ValueGeneratedNever();

        builder.Property(order => order.AccountId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(order => order.InstrumentId)
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(order => order.Direction)
            .HasConversion<string>()
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(order => order.Status)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(order => order.Quantity)
            .HasColumnType("numeric(18,8)")
            .IsRequired();

        builder.Property(order => order.RequestedPrice)
            .HasColumnType("numeric(18,10)")
            .IsRequired();

        builder.Property(order => order.SubmittedAtUtc)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(order => order.AcceptedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(order => order.RejectedAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(order => order.CancelledAtUtc)
            .HasColumnType("timestamp with time zone");

        builder.Property(order => order.Version)
            .IsConcurrencyToken()
            .ValueGeneratedNever()
            .IsRequired();

        builder.Property(order => order.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(order => order.UpdatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasIndex(order => order.AccountId);
        builder.HasIndex(order => order.InstrumentId);
        builder.HasIndex(order => order.Status);

        builder.Ignore(order => order.DomainEvents);
    }
}