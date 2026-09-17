namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

/// <summary>
/// Immutable template for a projectile's shape -- shared by every launch of the same kind of
/// shot (every Arrow, every Fireball), mirroring how Item/Spell/CharacterClass are shared
/// catalog templates rather than per-instance state. The actual damage/effects payload is
/// resolved once per launch onto a ProjectileInstance instead of living here, since that
/// depends on the specific wielder/weapon/ammo/spell combination, not just "what an arrow
/// looks like."
/// </summary>
public class ProjectileDefinition
{
    public string Name { get; }
    public int Range { get; }

    /// <summary>Additional creatures beyond the first this projectile can pass through -- 0 means it stops at the first hit. See ProjectileInstance.RemainingPenetration.</summary>
    public int Penetration { get; }

    /// <summary>Null means "derive the glyph from travel direction instead" -- see GlyphFor. Magical projectiles typically set this (a single glyph regardless of direction); physical ammo/thrown weapons typically leave it null so they visually orient to their flight path.</summary>
    public char? DisplayCharacter { get; }

    public ConsoleColor Color { get; }
    public AttackType AttackType { get; }

    public ProjectileDefinition(string name, int range, int penetration, char? displayCharacter, ConsoleColor color, AttackType attackType)
    {
        Name = name;
        Range = range;
        Penetration = penetration;
        DisplayCharacter = displayCharacter;
        Color = color;
        AttackType = attackType;
    }

    /// <summary>The glyph actually drawn this frame -- DisplayCharacter if set, otherwise derived from the direction of travel (North/South '|', East/West '-', diagonals '/' or '\').</summary>
    public char GlyphFor((int Dx, int Dy) direction) => DisplayCharacter ?? direction switch
    {
        (0, -1) or (0, 1) => '|',
        (-1, 0) or (1, 0) => '-',
        (1, -1) or (-1, 1) => '/',
        (-1, -1) or (1, 1) => '\\',
        _ => '*'
    };
}
