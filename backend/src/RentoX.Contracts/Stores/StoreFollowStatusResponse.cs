namespace RentoX.Contracts.Stores;

public sealed record StoreFollowStatusResponse(
    Guid StoreId,
    int FollowerCount,
    bool IsFollowing);