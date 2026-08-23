namespace RentoX.Application.Stores;

public sealed record StoreFollowStatusResult(
    Guid StoreId,
    int FollowerCount,
    bool IsFollowing);