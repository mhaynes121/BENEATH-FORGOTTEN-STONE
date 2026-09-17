using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

/// <summary>Glyph/color shown for a spell projectile's flight -- keyed off DamageType rather than the individual Spell, so every existing and future damage spell (see Spell.IsProjectile) gets a sensible animation with zero catalog edits. Shared by GameLoop.HandleCastSpell (player casts) and SpellCasterAI (monster casts) so both look identical.</summary>
public static class ProjectileVisuals
{
    public static (char Glyph, ConsoleColor Color) ForDamageType(DamageType type) => type switch
    {
        DamageType.Fire => ('*', ConsoleColor.Red),
        DamageType.Ice => ('*', ConsoleColor.Cyan),
        DamageType.Lightning => ('~', ConsoleColor.Yellow),
        DamageType.Poison => ('*', ConsoleColor.Green),
        DamageType.Holy => ('*', ConsoleColor.White),
        DamageType.Shadow => ('*', ConsoleColor.DarkMagenta),
        DamageType.Arcane => ('*', ConsoleColor.Magenta),
        _ => ('*', ConsoleColor.Gray)
    };
}
