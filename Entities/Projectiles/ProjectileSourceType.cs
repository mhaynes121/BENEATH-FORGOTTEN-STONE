namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

/// <summary>What kind of action created a projectile -- lets future mechanics (reflection, faction rules, UI) distinguish sources without a separate projectile system per source. See "Projectile System" doc section 7.</summary>
public enum ProjectileSourceType
{
    Weapon,
    Spell,
    Skill,
    MonsterAbility
}
