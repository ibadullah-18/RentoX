namespace RentoX.Application.Authorization;

public interface IIdentitySeeder
{
    Task SeedAsync(
        CancellationToken cancellationToken = default);
}