using DigiTrade.SharedKernel.Abstractions;

namespace Identity.Domain.Users;

public sealed class UserDomainService : IDomainService
{
    public User Create(
        Guid userId,
        string userName,
        string normalizedUserName,
        string email,
        string normalizedEmail,
        string passwordHash,
        DateTimeOffset createdAtUtc)
    {
        return new User
        {
            Id = userId,
            UserName = userName,
            NormalizedUserName = normalizedUserName,
            Email = email,
            NormalizedEmail = normalizedEmail,
            PasswordHash = passwordHash,
            Version = 1,
            CreatedAt = createdAtUtc,
            UpdatedAt = createdAtUtc,
        };
    }
}