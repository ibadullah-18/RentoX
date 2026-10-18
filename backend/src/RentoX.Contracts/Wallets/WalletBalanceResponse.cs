namespace RentoX.Contracts.Wallets;

public sealed record WalletBalanceResponse(
    Guid WalletId,
    Guid UserId,
    decimal Balance,
    string Currency)
{
    /// <summary>RentoX pays the fee for a listing over the free limit.</summary>
    public bool FreeListingFee { get; init; }

    /// <summary>RentoX pays for the VIP of a listing.</summary>
    public bool FreeVip { get; init; }

    /// <summary>RentoX pays for moving a listing up.</summary>
    public bool FreeBump { get; init; }

    /// <summary>RentoX pays for the VIP place of the store.</summary>
    public bool FreeStoreVip { get; init; }
}
