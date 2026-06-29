using Identity.Domain.Users;

namespace Identity.Application.Abstractions;

public interface IUserRepository
{
    Task<bool> ExistsByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default);

    Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default);

    Task<User?> FindByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default);

    Task AddAsync(User user, CancellationToken cancellationToken = default);
}