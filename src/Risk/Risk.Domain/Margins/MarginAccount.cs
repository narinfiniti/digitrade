using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Risk.Domain.Margins;

public sealed class MarginAccount : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public string AccountId { get; internal set; } = string.Empty;

    public string CurrencyCode { get; internal set; } = string.Empty;

    public decimal TotalMargin { get; internal set; }

    public decimal ReservedMargin { get; internal set; }

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = [];

    public void ClearDomainEvents()
    {
        DomainEvents = [];
    }
}