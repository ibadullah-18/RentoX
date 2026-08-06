namespace RentoX.Application.Abstractions.Authentication;

public interface ICurrentUserContext
{
    Guid? UserId { get; }

    bool IsAuthenticated { get; }
}