namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Player-facing names for an ammunition family -- shared by GameLoop's firing-error messages ("Arrows require a bow.") and InventoryScreen's examine-screen compatibility lines ("Compatible with: Bow"), so both say the same thing.</summary>
public static class AmmunitionTypeExtensions
{
    public static string PluralName(this AmmunitionType type) => type switch
    {
        AmmunitionType.Arrow => "Arrows",
        AmmunitionType.Bolt => "Bolts",
        AmmunitionType.SlingStone => "Sling Stones",
        _ => type.ToString()
    };

    /// <summary>"a bow" -- the article-inclusive launcher name this ammo family requires, for a sentence like "Arrows require {0}."</summary>
    public static string RequiredLauncherPhrase(this AmmunitionType type) => type switch
    {
        AmmunitionType.Arrow => "a bow",
        AmmunitionType.Bolt => "a crossbow",
        AmmunitionType.SlingStone => "a sling",
        _ => "a compatible launcher"
    };

    public static string LauncherName(this AmmunitionType type) => type switch
    {
        AmmunitionType.Arrow => "Bow",
        AmmunitionType.Bolt => "Crossbow",
        AmmunitionType.SlingStone => "Sling",
        _ => type.ToString()
    };
}
