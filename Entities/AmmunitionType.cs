namespace BENEATH_FORGOTTEN_STONE.Entities;

/// <summary>Which family of ammunition a ranged weapon fires, and which family an ammo item belongs to -- see Item.RequiredAmmunitionType/AmmunitionType, GameLoop.HandleFireProjectile. A Bow only ever consumes Arrow-family ammo, a Crossbow only Bolt-family, regardless of the specific item (e.g. a Fire Arrow and a plain Arrow are both AmmunitionType.Arrow).</summary>
public enum AmmunitionType
{
    Arrow,
    Bolt,

    /// <summary>A Sling's proper ammunition -- see Items.SlingStone. A plain Rock is ALSO tagged with this AmmunitionType (in addition to being independently throwable) so it can be fired from a Sling too, with a centralized accuracy/range penalty -- see Entities.Projectiles.ProjectileFactory.</summary>
    SlingStone
}
