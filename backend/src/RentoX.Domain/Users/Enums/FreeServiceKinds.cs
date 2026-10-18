namespace RentoX.Domain.Users.Enums;

/// <summary>Which paid services RentoX pays for a person. The owner picks them one by one.</summary>
[Flags]
public enum FreeServiceKinds
{
    None = 0,

    /// <summary>The fee for a listing over the free limit.</summary>
    ListingFee = 1,

    /// <summary>VIP for a listing.</summary>
    Vip = 2,

    /// <summary>Moving a listing up.</summary>
    Bump = 4,

    /// <summary>The VIP place of the store on the home page.</summary>
    StoreVip = 8,

    All = ListingFee | Vip | Bump | StoreVip
}
