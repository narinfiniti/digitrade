using Identity.Application.Abstractions;
using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence.Users;

public sealed class UserRepository(IdentityDbContext dbContext) : IUserRepository
{
    public Task<bool> ExistsByUserNameAsync(string normalizedUserName, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.NormalizedUserName == normalizedUserName, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.NormalizedEmail == normalizedEmail, cancellationToken);
    }

    public Task<User?> FindByLoginAsync(string normalizedLogin, CancellationToken cancellationToken = default)
    {
        return dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.NormalizedUserName == normalizedLogin || user.NormalizedEmail == normalizedLogin,
                cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
