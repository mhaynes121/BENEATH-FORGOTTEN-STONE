namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

public enum DamageType
{
    Physical,
    Fire,
    Ice,
    Lightning,
    Poison,
    Arcane,
    Shadow,
    Holy,

    /// <summary>No existing spell/skill/item deals this yet -- added for FloorTypeDefinition's WaterDamageMultiplier and future water-themed content. See Dungeon/FloorDamageCalculator.</summary>
    Water,

    /// <summary>No existing spell/skill/item deals this yet -- added (with Air) for the tile-attuned monster system's Mud/Sand-aligned creatures. See ElementalOpposition and Entities/FloorDamageCalculator.</summary>
    Earth,

    /// <summary>See Earth's own doc comment.</summary>
    Air
}
