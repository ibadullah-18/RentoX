using System.Security.Claims;

namespace RentoX.Application.Authorization;

/// <summary>
/// The parts of the admin panel an Admin can be limited to. A SuperAdmin has
/// them all. An Admin with no section chosen has them all too (the way every
/// admin worked before sections existed); choosing some limits the Admin to those.
/// </summary>
public static class AdminSections
{
    public const string ClaimType = "rx_section";

    public const string Listings = "listings";
    public const string Stores = "stores";
    public const string Reports = "reports";
    public const string Support = "support";
    public const string Catalog = "catalog";
    public const string Users = "users";
    public const string Finance = "finance";

    /// <summary>
    /// Looking after the staff: who came to work, the days off, the requests. Unlike the other sections it is never
    /// implied by "no section chosen": it must be given to an Admin on purpose (and a SuperAdmin has it).
    /// </summary>
    public const string Hr = "hr";

    /// <summary>
    /// The look of the site: name, logos, notice, motion. Like the staff section it must be given on purpose
    /// (no section chosen does not include it); a SuperAdmin has it.
    /// </summary>
    public const string Branding = "branding";

    /// <summary>The order matters: the position is the bit used when a set is recorded in the audit log.</summary>
    public static readonly string[] All =
        [Listings, Stores, Reports, Support, Catalog, Users, Finance, Hr, Branding];

    /// <summary>Sections an Admin has only when they were chosen for them.</summary>
    private static readonly string[] OnPurposeOnly = [Hr, Branding];

    /// <summary>The ordinary sections: what "no section chosen" stands for.</summary>
    public static IReadOnlyList<string> Ordinary { get; } = All.Where(s => !OnPurposeOnly.Contains(s)).ToArray();

    public static string Policy(string section) => $"section:{section}";

    public const string ListingsPolicy = "section:listings";
    public const string StoresPolicy = "section:stores";
    public const string ReportsPolicy = "section:reports";
    public const string SupportPolicy = "section:support";
    public const string CatalogPolicy = "section:catalog";
    public const string UsersPolicy = "section:users";
    public const string FinancePolicy = "section:finance";
    public const string HrPolicy = "section:hr";
    public const string BrandingPolicy = "section:branding";

    public static bool Allows(ClaimsPrincipal user, string section)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.IsInRole(RoleNames.SuperAdmin))
        {
            return true;
        }

        if (!user.IsInRole(RoleNames.Admin))
        {
            return false;
        }

        var chosen = user.FindAll(ClaimType).Select(c => c.Value).ToArray();
        if (OnPurposeOnly.Contains(section))
        {
            return chosen.Contains(section);
        }

        return chosen.Length == 0 || chosen.Contains(section);
    }

    /// <summary>A set of sections as one number (bit per section); an empty set means all of them.</summary>
    public static int ToMask(IEnumerable<string> sections)
    {
        string[] chosen = sections.ToArray();
        if (chosen.Length == 0)
        {
            // "No section chosen" is every section except the ones that must be given on purpose.
            int implied = 0;
            for (int i = 0; i < All.Length; i++)
            {
                if (!OnPurposeOnly.Contains(All[i]))
                {
                    implied |= 1 << i;
                }
            }

            return implied;
        }

        int mask = 0;
        for (int i = 0; i < All.Length; i++)
        {
            if (chosen.Contains(All[i]))
            {
                mask |= 1 << i;
            }
        }

        return mask;
    }
}
