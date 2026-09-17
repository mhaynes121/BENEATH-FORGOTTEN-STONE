using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

/// <summary>
/// One in-flight launch of a ProjectileDefinition -- all the state that changes as it travels,
/// plus the fully-resolved damage/effects payload computed once at launch (Damage/DamageType/
/// StatusEffects/etc. below) so collision resolution never needs to re-derive it from the
/// original weapon/spell/skill. See ProjectileDefinition's own doc comment for the Definition-
/// vs-Instance split this mirrors.
/// </summary>
public class ProjectileInstance
{
    public ProjectileDefinition Definition { get; }
    public Actor Source { get; }
    public ProjectileSourceType SourceType { get; }

    public int X { get; set; }
    public int Y { get; set; }
    public (int Dx, int Dy) Direction { get; }
    public int DistanceTraveled { get; set; }
    public int RemainingPenetration { get; set; }

    /// <summary>Never damage the same creature twice from one launch, even when penetration lets the projectile continue past it.</summary>
    public HashSet<Actor> AlreadyHit { get; } = new();

    /// <summary>Combat/status-effect text accumulated during flight (one or more entries when penetration hits several targets) -- the caller (GameLoop, RangedAttackAI) queues these into its own status-message history once Launch returns, since ProjectileEngine has no history of its own to write into.</summary>
    public List<string> Messages { get; } = new();

    // Resolved payload -- computed once at launch (GameLoop.HandleFireProjectile, or the
    // SpellCaster/SpellCasterAI projectile callback) and reused unchanged at every collision.
    public int Damage { get; set; }
    public DamageType DamageType { get; set; }
    public IReadOnlyList<ItemStatusEffect> StatusEffects { get; set; } = Array.Empty<ItemStatusEffect>();
    public bool IsBlessed { get; set; }
    public int BlessedUndeadDamageBonus { get; set; }

    /// <summary>Non-null for a thrown weapon OR fired ammo -- the actual single-charge unit cloned off the stack for this shot, so it can be run through ItemLandingResolver (and a thrown weapon's ThrownWeaponBehavior) once the shot resolves. Null only for spells, which carry no recoverable physical payload.</summary>
    public Item PayloadItem { get; set; }
    public ThrownWeaponBehavior? ThrownWeaponBehavior { get; set; }

    public ProjectileInstance(ProjectileDefinition definition, Actor source, ProjectileSourceType sourceType, int startX, int startY, (int Dx, int Dy) direction)
    {
        Definition = definition;
        Source = source;
        SourceType = sourceType;
        X = startX;
        Y = startY;
        Direction = direction;
        RemainingPenetration = definition.Penetration;
    }
}
