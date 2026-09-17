namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

public enum ProjectileTerminationReason
{
    HitWall,
    HitCreature,

    /// <summary>Reached max range without hitting anything -- not a per-hit accuracy miss, since a geometric collision is always resolved as a hit.</summary>
    Miss
}

/// <summary>Result of one ProjectileEngine.Launch call -- lets the caller (GameLoop, RangedAttackAI) decide follow-up (e.g. a thrown weapon's ThrownWeaponBehavior needs the final tile) without the engine itself needing to know about inventory/UI concerns.</summary>
public class ProjectileOutcome
{
    public ProjectileTerminationReason Reason { get; }
    public ProjectileInstance Projectile { get; }

    private ProjectileOutcome(ProjectileTerminationReason reason, ProjectileInstance projectile)
    {
        Reason = reason;
        Projectile = projectile;
    }

    public static ProjectileOutcome HitWall(ProjectileInstance p) => new(ProjectileTerminationReason.HitWall, p);
    public static ProjectileOutcome HitCreature(ProjectileInstance p) => new(ProjectileTerminationReason.HitCreature, p);
    public static ProjectileOutcome Miss(ProjectileInstance p) => new(ProjectileTerminationReason.Miss, p);
}
