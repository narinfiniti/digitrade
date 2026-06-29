using DigiTrade.SharedKernel.Abstractions;
using DigiTrade.SharedKernel.Events;

namespace Identity.Domain.Users;

public sealed class User : IAggregateRoot<Guid>
{
    public Guid Id { get; internal set; }

    public string UserName { get; internal set; } = string.Empty;

    public string NormalizedUserName { get; internal set; } = string.Empty;

    public string Email { get; internal set; } = string.Empty;

    public string NormalizedEmail { get; internal set; } = string.Empty;

    public string PasswordHash { get; internal set; } = string.Empty;

    public int Version { get; internal set; }

    public DateTimeOffset CreatedAt { get; internal set; }

    public DateTimeOffset UpdatedAt { get; internal set; }

    public IReadOnlyCollection<IDomainEvent> DomainEvents { get; internal set; } = Array.Empty<IDomainEvent>();

    public void ClearDomainEvents()
    {
        DomainEvents = Array.Empty<IDomainEvent>();
    }
}