namespace BENEATH_FORGOTTEN_STONE.Entities.Components;

public enum ItemType
{
    Consumable,
    Weapon,
    Armor,
    Wand,
    Scroll,
    Spellbook,
    Key,
    Lockpick,

    /// <summary>Consumed one-per-shot by a matching ranged weapon -- see Item.AmmunitionType/RequiredAmmunitionType, Core/ProjectileEngine.cs. Stock count is Item.Charges, same "stack" mechanism Lockpicks already use.</summary>
    Ammunition,

    /// <summary>Candle/Torch/Lantern -- see Item.EmitsLight and friends, InventoryScreen.ApplyItem's light/extinguish toggle. Deliberately excluded from ItemStacking.IsStackable: each instance carries its own independent IsLit/RemainingLightDuration, which a shared Charges-style stack can't represent.</summary>
    LightSource,

    /// <summary>A portable bag -- see Item.Container/ContainerComponent. Every catalog entry of this type must set container: non-null; see Entities/ContainerCatalog.cs.</summary>
    Container,

    /// <summary>Corpse System: an emptied corpse's portable form -- see Item.CorpseMetadata, CorpseItemFactory. Never appears in the Items.cs catalog (built fresh per death, never authored/shared); deliberately excluded from ItemStacking.IsStackable's list since every corpse must keep its own independent identity.</summary>
    Corpse
}
