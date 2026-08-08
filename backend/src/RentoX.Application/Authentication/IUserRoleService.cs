namespace RentoX.Application.Authorization;

public interface IUserRoleService
{
    Task AssignDefaultRoleAsync(
        Guid userId,
        CancellationToken cancellationToken = default);
}