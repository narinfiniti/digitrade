using Identity.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace Identity.Persistence;

public sealed class IdentityDbContext(DbContextOptions<IdentityDbContext> options) : DbContext(options)
{
    public const string DefaultSchema = "identity";

    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.HasDefaultSchema(DefaultSchema);

        var user = modelBuilder.Entity<User>();
        user.ToTable("users");
        user.HasKey(entity => entity.Id);
        user.Property(entity => entity.Id).HasColumnName("id");
        user.Property(entity => entity.UserName).HasColumnName("user_name").IsRequired();
        user.Property(entity => entity.NormalizedUserName).HasColumnName("normalized_user_name").IsRequired();
        user.Property(entity => entity.Email).HasColumnName("email").IsRequired();
        user.Property(entity => entity.NormalizedEmail).HasColumnName("normalized_email").IsRequired();
        user.Property(entity => entity.PasswordHash).HasColumnName("password_hash").IsRequired();
        user.Property(entity => entity.Version).HasColumnName("version");
        user.Property(entity => entity.CreatedAt).HasColumnName("created_at");
        user.Property(entity => entity.UpdatedAt).HasColumnName("updated_at");
        user.Ignore(entity => entity.DomainEvents);

        user.HasIndex(entity => entity.NormalizedUserName)
            .IsUnique();
        user.HasIndex(entity => entity.NormalizedEmail)
            .IsUnique();
    }
}
