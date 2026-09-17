using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Skills;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;

namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Stats + inventory view opened with 'i' during play, split into three
/// tabs (Left/Right arrow keys cycle, with wraparound) rather than one long
/// scrolling page -- Character (identity/stats/equipment), Inventory (the
/// interactive one), and Spells &amp; Skills last (both purely informational;
/// spells cast via 'c' and skills used via 'k' from the map, never from
/// here). Left/Right/Esc/U/L/D all work from any tab since none of them
/// depend on what's currently on screen (they open their own sub-menus);
/// only the raw digit/letter quick-select (equip/use the item at that
/// on-screen slot) is Inventory-tab-specific, since with the count cap gone
/// (see EncumbranceCalculator) inventory can legitimately exceed 9 items,
/// so 'd'/'l'/'u' are real, reachable item slots -- hence those three
/// commands living on the Shift'd uppercase letter instead (see the
/// type-level note that used to live here, now just: case-sensitive on
/// purpose).
/// </summary>
public static class InventoryScreen
{
    private static readonly string[] TabNames = { "Character", "Inventory", "Spells & Skills" };
    private const int InventoryTab = 1;
    private const int SpellsSkillsTab = 2;

    /// <summary>Max rows shown at once for either the Spells or the Skills list on the Spells &amp; Skills tab -- each list pages independently (PageUp/PageDown for spells, Up/Down for skills) rather than sharing one page counter, so paging a long spell list never disturbs where you are in the skill list. Internal (not private) so Diagnostics/SelfTest.cs can verify pagination stays within this limit.</summary>
    internal const int SpellSkillPageSize = 15;

    /// <summary>
    /// Returns whether a turn-costing action happened while the screen was open. Every ordinary
    /// equip/unequip/drop/examine stays free, matching this screen's long-standing behavior --
    /// only equipping, replacing, or removing RangedWeapon/Ammunition specifically costs a turn
    /// (design doc section 7). Detected by comparing both slots' occupant before/after each
    /// action that could touch them, rather than changing ApplyItem/TryUnequip's own signatures
    /// (both stay plain string-returning helpers, used directly by Diagnostics/SelfTest.cs).
    /// </summary>
    public static bool Show(Player player, Level level)
    {
        string message = "";
        int tab = 0;
        int spellsPage = 0;
        int skillsPage = 0;
        bool turnConsumed = false;

        bool RangedOrAmmoChanged(Item beforeRanged, Item beforeAmmo) =>
            player.Equipment.Get(EquipmentSlot.RangedWeapon) != beforeRanged || player.Equipment.Get(EquipmentSlot.Ammunition) != beforeAmmo;

        using var margin = new ScreenMargin();
        while (true)
        {
            ConsoleSafety.TryClear();
            ConsoleSafety.TrySetCursorPosition(0, 0);

            Console.WriteLine($"=== Character Sheet: {TabNames[tab]} ({tab + 1}/{TabNames.Length}) ===");
            Console.WriteLine();

            int spellsPageCount = 1;
            int skillsPageCount = 1;
            switch (tab)
            {
                case 0:
                    ShowCharacterTab(player);
                    break;
                case SpellsSkillsTab:
                    (spellsPageCount, skillsPageCount) = ShowSpellsAndSkillsTab(player, level, spellsPage, skillsPage);
                    break;
                case InventoryTab:
                    ShowInventoryTab(player);
                    break;
            }

            Console.WriteLine();
            if (!string.IsNullOrEmpty(message))
            {
                // Word-wrapped at the actual window width, not left to the console's own
                // auto-wrap -- writing all the way to the last column is what was clipping a
                // few characters off the first line (same reason Renderer.RenderStatusBar
                // wraps its own messages instead of relying on Console.WriteLine directly).
                foreach (string line in TextWrapper.WrapText(message, GetScreenWidth()))
                {
                    Console.WriteLine(line);
                }
                Console.WriteLine();
            }

            Console.WriteLine("Left/Right: switch view   Esc: close");
            if (tab == SpellsSkillsTab && (spellsPageCount > 1 || skillsPageCount > 1))
            {
                Console.WriteLine("PageUp/PageDown: spells page   Up/Down: skills page   U: unequip   L: examine   D: drop   C: compare   O: open container");
            }
            else
            {
                Console.WriteLine(tab == InventoryTab
                    ? "1-9/a-z: use or equip   U: unequip   L: examine   D: drop   C: compare   O: open container"
                    : "U: unequip   L: examine   D: drop   C: compare   O: open container");
            }

            var key = Console.ReadKey(intercept: true);
            if (key.Key == ConsoleKey.Escape)
            {
                // Renderer.Render only redraws the map's own footprint (Width columns,
                // Height rows) -- this screen's much wider/taller text would otherwise
                // leave stray characters behind once the game view resumes.
                ConsoleSafety.TryClear();
                return turnConsumed;
            }
            if (key.Key == ConsoleKey.LeftArrow)
            {
                tab = (tab - 1 + TabNames.Length) % TabNames.Length;
                spellsPage = 0;
                skillsPage = 0;
                message = "";
                continue;
            }
            if (key.Key == ConsoleKey.RightArrow)
            {
                tab = (tab + 1) % TabNames.Length;
                spellsPage = 0;
                skillsPage = 0;
                message = "";
                continue;
            }
            if (tab == SpellsSkillsTab && key.Key == ConsoleKey.PageUp)
            {
                spellsPage = Math.Max(0, spellsPage - 1);
                continue;
            }
            if (tab == SpellsSkillsTab && key.Key == ConsoleKey.PageDown)
            {
                spellsPage = Math.Min(spellsPageCount - 1, spellsPage + 1);
                continue;
            }
            if (tab == SpellsSkillsTab && key.Key == ConsoleKey.UpArrow)
            {
                skillsPage = Math.Max(0, skillsPage - 1);
                continue;
            }
            if (tab == SpellsSkillsTab && key.Key == ConsoleKey.DownArrow)
            {
                skillsPage = Math.Min(skillsPageCount - 1, skillsPage + 1);
                continue;
            }

            // Case-sensitive on purpose: lowercase a-z are real item slots on the Inventory
            // tab (up to 35 with 1-9), so these three only fire on the Shift'd uppercase
            // letter -- otherwise 'd'/'l'/'u' would be permanently unreachable item slots
            // the moment an inventory grows past 9 items.
            char keyChar = key.KeyChar;
            if (keyChar == 'U')
            {
                Item beforeRanged = player.Equipment.Get(EquipmentSlot.RangedWeapon);
                Item beforeAmmo = player.Equipment.Get(EquipmentSlot.Ammunition);
                message = UnequipFlow(player);
                if (RangedOrAmmoChanged(beforeRanged, beforeAmmo))
                {
                    turnConsumed = true;
                }
                continue;
            }
            if (keyChar == 'L')
            {
                message = LookFlow(player);
                continue;
            }
            if (keyChar == 'D')
            {
                message = DropFlow(player, level);
                continue;
            }
            if (keyChar == 'C')
            {
                ItemComparisonScreen.ShowInventoryCompare(player);
                continue;
            }
            if (keyChar == 'O')
            {
                message = OpenContainerFlow(player, level);
                continue;
            }

            if (tab != InventoryTab)
            {
                continue;
            }

            // Only non-container items are quick-select-able by number/letter here -- a carried
            // bag is opened via 'O' instead (its own MenuPrompt selection), so it's excluded from
            // this compact numbering the same way ShowInventoryTab's own display excludes it.
            var items = NonContainerInventoryItems(player);
            int index = keyChar >= '1' && keyChar <= '9' ? keyChar - '1'
                : keyChar >= 'a' && keyChar <= 'z' ? 9 + (keyChar - 'a')
                : -1;

            if (index >= 0 && index < items.Count)
            {
                Item beforeRanged = player.Equipment.Get(EquipmentSlot.RangedWeapon);
                Item beforeAmmo = player.Equipment.Get(EquipmentSlot.Ammunition);
                message = ApplyItem(player, items[index], level);
                if (RangedOrAmmoChanged(beforeRanged, beforeAmmo))
                {
                    turnConsumed = true;
                }
            }
            else
            {
                message = "";
            }
        }
    }

    private static void ShowCharacterTab(Player player)
    {
        Console.WriteLine($"Name: {player.Name}   Race: {player.Race.Name}   Class: {player.Class.Name}");

        Console.WriteLine(
            $"Level: {player.Level}   Turn: {player.TurnCount}   Gold: {player.Gold}   " +
            $"HP: {player.Health.Current}/{player.Health.Max}   MP: {player.Mana.Current}/{player.Mana.Max}");

        // Reuses the same derived combat stats the rest of the game uses -- see Player.RecalculateDerivedStats
        Console.WriteLine($"Attack: {player.BasePhysicalAttackPower} phys / {player.BaseMagicalAttackPower} magic   Defense: {player.DefensePower}");

        // Adjusted (base+race+class+equipment+other), not raw rolled values
        Console.WriteLine(
            $"STR: {player.Stats.Adjusted(PrimaryAttribute.Strength)}   " +
            $"CON: {player.Stats.Adjusted(PrimaryAttribute.Constitution)}   " +
            $"AGI: {player.Stats.Adjusted(PrimaryAttribute.Agility)}   " +
            $"WIS: {player.Stats.Adjusted(PrimaryAttribute.Wisdom)}   " +
            $"KNO: {player.Stats.Adjusted(PrimaryAttribute.Knowledge)}   " +
            $"CHA: {player.Stats.Adjusted(PrimaryAttribute.Charisma)}   " +
            $"LCK: {player.Stats.Adjusted(PrimaryAttribute.Luck)}");

        // Current EFFECTIVE resistance (race+class+stat+equipment+buffs/debuffs, clamped) --
        // required directly on this tab per the Resistance System spec (section 42): the player
        // must never need a separate menu to see it, and it must always reflect what's actually
        // equipped/active right now, not a cached or base value. Nothing here is cached --
        // GetEffectiveResistance recomputes live on every call, so equipping/unequipping gear
        // or a buff expiring is reflected the next time this screen renders, same as HP/MP/Attack.
        Console.WriteLine("Resists: " + FormatResistLine(player));

        Console.WriteLine($"Weight: {EncumbranceCalculator.CurrentWeight(player):0.#} / {EncumbranceCalculator.MaxCapacity(player):0.#} lbs");
        Console.WriteLine();

        Console.WriteLine("Equipment:");
        Console.WriteLine();
        var equipmentCells = new List<string>();
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            var equipped = player.Equipment.Get(slot);
            string itemName = equipped != null ? equipped.DisplayName + BlessedCursedTag(equipped) + EquippedQuantitySuffix(equipped) : "[Empty]";
            equipmentCells.Add($"{EquipmentCompatibility.SlotLabel(slot) + ":",-15}{itemName}");
        }
        RenderTwoColumns(equipmentCells);
    }

    /// <summary>" (17)" for a stacked equipped item (only ever possible in Ammo/Thrown -- see ItemStacking.IsStackable), empty for the single-unit case every other slot always is.</summary>
    private static string EquippedQuantitySuffix(Item item) =>
        item.Charges is > 1 && ItemStacking.IsStackable(item) ? $" ({item.Charges})" : "";

    /// <summary>
    /// Shows the FULL roster the class can ever reach -- every CanBeCastBy spell / every
    /// AllowedClasses skill, not just what's already known -- so a player can see what's
    /// still ahead and at what level, not just what they already have. Colored with the same
    /// bright/dim/locked scheme TraderScreen already uses (ItemLineColor): bright = known and
    /// usable right now, dim = known but out of mana/on cooldown, locked/red = not learned yet.
    /// Skills are automatically granted by level (Skill.Level IS the grant trigger -- see
    /// Player.GrantSkillsForLevel), so "known" there just means player.Level has reached it.
    /// Spells are never level-gated automatically -- KnownSpells only grows by reading a
    /// scroll/spellbook -- so an unknown spell's Level is a minimum to learn, not a promise.
    /// </summary>
    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can verify pagination directly against the real catalog instead of a synthetic list.</summary>
    internal static List<Spell> GetVisibleSpells(Player player) =>
        !player.Class.IsSpellcaster
            ? new List<Spell>()
            : SpellCatalog.All.Where(s => s.CanBeCastBy(player.Class))
                .OrderBy(s => s.Level).ThenBy(s => s.Name).ToList();

    /// <summary>Internal (not private) -- see GetVisibleSpells.</summary>
    internal static List<Skill> GetVisibleSkills(Player player) =>
        SkillCatalog.All.Where(s => s.AllowedClasses.Contains(player.Class))
            .OrderBy(s => s.LevelFor(player.Class)).ThenBy(s => s.Name).ToList();

    /// <summary>Internal (not private) -- see GetVisibleSpells.</summary>
    internal static int PageCount(int itemCount) => Math.Max(1, (int)Math.Ceiling(itemCount / (double)SpellSkillPageSize));

    /// <summary>Returns (spellsPageCount, skillsPageCount) so the caller can clamp PageUp/PageDown/Left/Right and decide whether to show the paging hint footer.</summary>
    private static (int SpellsPageCount, int SkillsPageCount) ShowSpellsAndSkillsTab(Player player, Level level, int spellsPage, int skillsPage)
    {
        var spells = GetVisibleSpells(player);
        var skills = GetVisibleSkills(player);
        int spellsPageCount = PageCount(spells.Count);
        int skillsPageCount = PageCount(skills.Count);

        Console.WriteLine("Spells:");
        if (!player.Class.IsSpellcaster)
        {
            Console.WriteLine("  (Your class cannot cast spells.)");
        }
        else
        {
            foreach (var spell in spells.Skip(spellsPage * SpellSkillPageSize).Take(SpellSkillPageSize))
            {
                bool known = player.KnownSpells.Contains(spell);
                bool ready = known && player.Mana.Current >= spell.Casting.ManaCost
                    && !(player.SpellCooldowns.TryGetValue(spell, out int readyTurn) && level.TurnNumber < readyTurn);
                string rankSuffix = known && !string.IsNullOrEmpty(spell.ProficiencyId) ? $" ({player.RankOf(spell.ProficiencyId)})" : "";
                string status = known ? $"known{rankSuffix}" : $"learn scroll/spellbook, level {spell.Level}+";

                Console.ForegroundColor = TraderScreen.ItemLineColor(canUse: known, canAfford: ready);
                Console.WriteLine($"  {spell.DisplayCharacter} {spell.Name,-20}{spell.Casting.ManaCost,3} MP  [{status}]");
                Console.ResetColor();
            }
            if (spellsPageCount > 1)
            {
                Console.WriteLine($"  -- Spells page {spellsPage + 1}/{spellsPageCount} --");
            }
        }
        Console.WriteLine();

        Console.WriteLine("Skills:");
        if (skills.Count == 0)
        {
            Console.WriteLine("  (Your class has no physical skills.)");
        }
        else
        {
            foreach (var skill in skills.Skip(skillsPage * SpellSkillPageSize).Take(SpellSkillPageSize))
            {
                bool known = player.KnownSkills.Contains(skill);
                bool ready = known && (skill.IsPassive
                    || !(player.SkillCooldowns.TryGetValue(skill, out int readyTurn) && level.TurnNumber < readyTurn));
                string rankSuffix = known && !string.IsNullOrEmpty(skill.ProficiencyId) ? $" ({player.RankOf(skill.ProficiencyId)})" : "";
                string status = !known ? $"learned at level {skill.LevelFor(player.Class)}" : skill.IsPassive ? $"passive, active{rankSuffix}" : $"ready{rankSuffix}";

                Console.ForegroundColor = TraderScreen.ItemLineColor(canUse: known, canAfford: ready);
                Console.WriteLine($"  {skill.DisplayCharacter} {skill.Name,-20}[{status}]");
                Console.ResetColor();
            }
            if (skillsPageCount > 1)
            {
                Console.WriteLine($"  -- Skills page {skillsPage + 1}/{skillsPageCount} --");
            }
        }
        Console.WriteLine();

        // Not a Skill/Spell (no level-gate, no cooldown, no SkillCatalog entry) -- throwing is
        // the universal 'F' command any class can use on a carried CanBeThrown item whose
        // ThrowableCategory it's allowed (see CharacterClass.AllowedThrowableCategories,
        // GameLoop.HandleFireProjectile). Listed here so a class's throwing access is
        // actually discoverable somewhere in the UI instead of only in the Help screen text.
        Console.WriteLine("Throwing (press F on the map):");
        var throwables = Items.All
            .Where(i => i.CanBeThrown && i.ThrowableCategory.HasValue && player.Class.AllowedThrowableCategories.Contains(i.ThrowableCategory.Value))
            .OrderBy(i => i.ThrowableCategory).ThenBy(i => i.Name)
            .ToList();
        if (throwables.Count == 0)
        {
            Console.WriteLine("  (Your class has no throwing training.)");
        }
        else
        {
            foreach (var item in throwables)
            {
                bool carried = player.Inventory.Items.Any(i => i.Name == item.Name);
                Console.ForegroundColor = carried ? ConsoleColor.White : ConsoleColor.DarkGray;
                Console.WriteLine($"  {item.Symbol} {item.Name,-20}[{item.ThrowableCategory}]{(carried ? "" : " -- none carried")}");
                Console.ResetColor();
            }
        }
        Console.WriteLine();

        Console.WriteLine("Active Effects:");
        var statusLines = FormatStatusFlags(player, level.TurnNumber);
        if (player.ActiveEffects.Count == 0 && statusLines.Count == 0)
        {
            Console.WriteLine("  (none)");
        }
        else
        {
            foreach (var effect in player.ActiveEffects)
            {
                Console.WriteLine($"  {FormatActiveEffect(effect, level.TurnNumber)}");
            }
            foreach (var line in statusLines)
            {
                Console.WriteLine($"  {line}");
            }
        }

        return (spellsPageCount, skillsPageCount);
    }

    /// <summary>Every carried item that isn't itself a container -- the compact, quick-select-able list at the top of the Inventory tab. Excludes carried bags, which get their own single-column section below and are opened via 'O' rather than a number/letter quick-select. Internal (not private) so Diagnostics/SelfTest.cs can verify the split directly.</summary>
    internal static List<Item> NonContainerInventoryItems(Player player) =>
        player.Inventory.Items.Where(i => i.Container == null).ToList();

    private static void ShowInventoryTab(Player player)
    {
        var items = player.Inventory.Items;
        Console.WriteLine($"Inventory ({items.Count} items):");
        Console.WriteLine();

        var nonContainerItems = NonContainerInventoryItems(player);
        if (nonContainerItems.Count == 0)
        {
            Console.WriteLine("  (empty)");
        }
        else
        {
            var inventoryCells = new List<string>();
            for (int i = 0; i < nonContainerItems.Count; i++)
            {
                // A stacked item (potion, scroll, ...) only shows its count once there's more
                // than one to distinguish -- "Health Potion" alone already implies "1". A
                // non-stackable charge item (a Wand) still always shows it, since "[1]" there
                // is a meaningful last-charge warning, not stack-size clutter.
                bool showCharges = nonContainerItems[i].Charges != null
                    && (!ItemStacking.IsStackable(nonContainerItems[i]) || nonContainerItems[i].Charges > 1);
                string charges = showCharges ? $" [{nonContainerItems[i].Charges}]" : "";
                inventoryCells.Add($"{MenuPrompt.OptionKey(i)}. {nonContainerItems[i].DisplayName}{BlessedCursedTag(nonContainerItems[i])}{charges}");
            }
            RenderTwoColumns(inventoryCells);
        }

        var containerItems = items.Where(i => i.Container != null).ToList();
        if (containerItems.Count > 0)
        {
            Console.WriteLine();
            Console.WriteLine("Containers:");
            foreach (var containerItem in containerItems)
            {
                var container = containerItem.Container;
                string reduction = container.WeightReduction > 0 ? $", {container.WeightReduction:0%} reduction" : "";
                Console.WriteLine($"  {containerItem.DisplayName} -- {container.Contents.Items.Count}/{container.SlotCapacity} slots{reduction}");
            }
        }
    }

    private static readonly (string Label, ResistanceType Type)[] ResistDisplayOrder =
    {
        ("Fir", ResistanceType.Fire), ("Wat", ResistanceType.Water), ("Ice", ResistanceType.Ice),
        ("Shk", ResistanceType.Shock), ("Poi", ResistanceType.Poison), ("Mag", ResistanceType.Magic)
    };

    /// <summary>"Fir +12  Wat 0  Ice +8  Shk -5  Poi +15  Mag +4" -- sign always shown for a nonzero value, bare "0" for exactly zero (Resistance System spec section 43's own compact format). Internal (not private) so Diagnostics/SelfTest.cs can verify the exact format directly.</summary>
    internal static string FormatResistLine(Player player) =>
        string.Join("  ", ResistDisplayOrder.Select(e =>
        {
            int value = player.GetEffectiveResistance(e.Type);
            string formatted = value switch { 0 => "0", > 0 => $"+{value}", _ => value.ToString() };
            return $"{e.Label} {formatted}";
        }));

    /// <summary>" [B]"/" [C]" for an identified Blessed/Cursed item, empty otherwise -- empty (not a hidden marker) while unidentified, since even the presence of a marker would leak that the item has something to hide.</summary>
    private static string BlessedCursedTag(Item item)
    {
        if (!item.IsIdentified)
        {
            return "";
        }
        if (item.IsBlessed) return " [B]";
        if (item.IsCursed) return " [C]";
        return "";
    }

    /// <summary>Renders cells two-per-row (cells[0]/cells[1] on row 0, cells[2]/cells[3] on row 1, ...), columns evenly divided by the current window width.</summary>
    private static void RenderTwoColumns(IReadOnlyList<string> cells)
    {
        int columnWidth = GetColumnWidth();
        for (int i = 0; i < cells.Count; i += 2)
        {
            string left = cells[i];
            string right = i + 1 < cells.Count ? cells[i + 1] : "";
            Console.WriteLine(string.IsNullOrEmpty(right) ? left : left.PadRight(columnWidth) + right);
        }
    }

    /// <summary>Reserves ScreenMargin's left+right margin on top of the raw window width, so wrapped text never runs into the new margin or past the true right edge.</summary>
    private static int GetScreenWidth()
    {
        try
        {
            return Math.Max(20, Console.WindowWidth - ScreenMargin.LeftMargin - ScreenMargin.RightMargin);
        }
        catch (IOException)
        {
            return 72;
        }
    }

    private static int GetColumnWidth()
    {
        try
        {
            return Math.Max(20, (Console.WindowWidth - ScreenMargin.LeftMargin - ScreenMargin.RightMargin) / 2);
        }
        catch (IOException)
        {
            return 40;
        }
    }

    private static string UnequipFlow(Player player)
    {
        var occupiedSlots = new List<EquipmentSlot>();
        foreach (var candidateSlot in Enum.GetValues<EquipmentSlot>())
        {
            if (player.Equipment.Get(candidateSlot) != null)
            {
                occupiedSlots.Add(candidateSlot);
            }
        }

        if (occupiedSlots.Count == 0)
        {
            return "You have nothing equipped.";
        }

        // No capacity check needed -- moving an item from Equipment to Inventory doesn't
        // change total carried weight (see EncumbranceCalculator), so it can never push
        // the player over capacity.
        var labels = occupiedSlots.Select(s => $"{EquipmentCompatibility.SlotLabel(s)}: {player.Equipment.Get(s).DisplayName}{BlessedCursedTag(player.Equipment.Get(s))}{EquippedQuantitySuffix(player.Equipment.Get(s))}").ToList();
        int? index = MenuPrompt.Choose("Unequip which slot?", labels, allowCancel: true);
        if (index == null)
        {
            return "Cancelled.";
        }

        return TryUnequip(player, occupiedSlots[index.Value]);
    }

    /// <summary>
    /// The actual unequip decision/action, split out from UnequipFlow's slot-selection
    /// prompt so it's exercisable without Console.ReadKey -- see Diagnostics/SelfTest.cs,
    /// which calls this directly instead of driving the interactive menu.
    /// </summary>
    internal static string TryUnequip(Player player, EquipmentSlot slot)
    {
        var equipped = player.Equipment.Get(slot);

        // A cursed item resists removal outright -- the wording stays deliberately vague
        // until the player has actually identified it, per Item.IsCursed's own doc comment.
        if (equipped.IsCursed)
        {
            return equipped.IsIdentified
                ? $"You try to remove the {equipped.Name}, but it will not come off."
                : "For some unknown reason, you cannot remove the item.";
        }

        var item = player.Unequip(slot);
        player.Inventory.AddItem(item);
        return $"You unequip the {item.DisplayName}.";
    }

    /// <summary>Drops a carried item onto the player's current tile -- mirrors GameLoop.HandleDropItem, only reachable from here for items in Inventory.Items (equipped items aren't listed; unequip first).</summary>
    private static string DropFlow(Player player, Level level)
    {
        if (player.Inventory.Items.Count == 0)
        {
            return "You have nothing to drop.";
        }

        var labels = player.Inventory.Items.Select(i => i.DisplayName + BlessedCursedTag(i)).ToList();
        int? index = MenuPrompt.Choose("Drop which item?", labels, allowCancel: true);
        if (index == null)
        {
            return "Cancelled.";
        }

        var item = player.Inventory.Items[index.Value];
        player.Inventory.RemoveItem(item);
        string destroyedMessage = ItemDestructionRules.LandOnFloor(level, player.X, player.Y, item);
        return destroyedMessage ?? $"You drop the {item.DisplayName}.";
    }

    /// <summary>Opens a carried bag's ContainerScreen -- always free, matching every other Inventory-screen action. A bag's own IsOpen is set true only while the screen is actually up (see ContainerScreen.Show's Esc handling, which resets it to false automatically on exit).</summary>
    private static string OpenContainerFlow(Player player, Level level)
    {
        var bags = player.Inventory.Items.Where(i => i.Container != null).ToList();
        if (bags.Count == 0)
        {
            return "You aren't carrying any containers.";
        }

        var labels = bags.Select(b => b.DisplayName).ToList();
        int? index = MenuPrompt.Choose("Open which container?", labels, allowCancel: true);
        if (index == null)
        {
            return "Cancelled.";
        }

        var bag = bags[index.Value];
        bag.Container.IsOpen = true;
        ContainerScreen.Show(player, bag.Container, bag.DisplayName, level);
        return "";
    }

    /// <summary>"Bless: +3 Physical Attack (5 turns left)" / "Burning: 2 Fire damage/turn (3 turns left)" -- covers both flavors of ActiveEffect (see StatModifierEffect/StatusEffect); currentTurn is Level.TurnNumber, so remaining is exact, never stale.</summary>
    private static string FormatActiveEffect(ActiveEffect effect, int currentTurn)
    {
        int turnsLeft = Math.Max(0, effect.ExpiresOnTurn - currentTurn);

        string detail = effect.ModifiedStat.HasValue
            ? $"{(effect.StatAmount >= 0 ? "+" : "")}{effect.StatAmount} {StatLabel(effect.ModifiedStat.Value)}"
            : effect.TickDamage > 0
                ? $"{effect.TickDamage} {effect.TickDamageType} damage/turn"
                : "";

        return string.IsNullOrEmpty(detail)
            ? $"{effect.SourceSpellName} ({turnsLeft} turns left)"
            : $"{effect.SourceSpellName}: {detail} ({turnsLeft} turns left)";
    }

    /// <summary>Stun/Silence/CC-immunity never become ActiveEffect entries (see Actor.StunnedUntilTurn's doc comment -- they're pass/fail, not a magnitude to revert), so they'd otherwise be invisible here. Synthetic lines only, appended to the same list.</summary>
    private static List<string> FormatStatusFlags(Player player, int currentTurn)
    {
        var lines = new List<string>();
        if (currentTurn < player.StunnedUntilTurn)
        {
            lines.Add($"Stunned ({player.StunnedUntilTurn - currentTurn} turns left)");
        }
        if (currentTurn < player.SilencedUntilTurn)
        {
            lines.Add($"Silenced ({player.SilencedUntilTurn - currentTurn} turns left)");
        }
        if (currentTurn < player.CcImmuneUntilTurn)
        {
            lines.Add($"Immune to stun/silence ({player.CcImmuneUntilTurn - currentTurn} turns left)");
        }
        // Prone has no duration/turn-countdown of its own (see Actor.IsProne's own doc comment) --
        // just a plain flag, unlike the three above.
        if (player.IsProne)
        {
            lines.Add("Prone (press T to stand)");
        }
        if (currentTurn < player.FrightenedUntilTurn)
        {
            lines.Add($"Frightened ({player.FrightenedUntilTurn - currentTurn} turns left)");
        }
        // Intercession has no turn-countdown shown here (same reasoning as Prone above) -- just
        // whether the protection is currently up.
        if (player.IntercessionActive)
        {
            lines.Add("Divine protection active (Intercession)");
        }
        return lines;
    }

    private static string StatLabel(Stat stat) => stat switch
    {
        Stat.PhysicalAttack => "Physical Attack",
        Stat.MagicalAttack => "Magical Attack",
        Stat.Armor => "Armor",
        Stat.MagicResistance => "Magic Resistance",
        Stat.MovementSpeed => "Movement Speed",
        Stat.CarryCapacity => "lbs Carry Capacity",
        _ => stat.ToString()
    };

    /// <summary>
    /// Look/examine within the inventory screen (distinct from the map's
    /// directional Look): lists every carried AND equipped item together --
    /// inspection doesn't care whether an item is currently worn, per the
    /// Look spec -- and shows its LongDescription plus full item details.
    /// </summary>
    private static string LookFlow(Player player)
    {
        var candidates = new List<(string Label, Item Item)>();
        foreach (var invItem in player.Inventory.Items)
        {
            candidates.Add((invItem.DisplayName + BlessedCursedTag(invItem), invItem));
        }
        foreach (var slot in Enum.GetValues<EquipmentSlot>())
        {
            var equipped = player.Equipment.Get(slot);
            if (equipped != null)
            {
                candidates.Add(($"{equipped.DisplayName}{BlessedCursedTag(equipped)} ({EquipmentCompatibility.SlotLabel(slot)})", equipped));
            }
        }

        if (candidates.Count == 0)
        {
            return "You have nothing to examine.";
        }

        var labels = candidates.Select(c => c.Label).ToList();
        int? index = MenuPrompt.Choose("Examine which item?", labels, allowCancel: true);
        return index == null ? "Cancelled." : FormatItemDetails(player, candidates[index.Value].Item);
    }

    /// <summary>
    /// Explicit examination always shows LongDescription (never ShortDescription) plus
    /// whatever stats/requirements are already player-visible elsewhere -- nothing new
    /// invented for this. Everything past the description is mechanical information, so an
    /// unidentified item stops right there -- no stats, no StatModifiers, no Blessed/Cursed,
    /// no StatusEffects, per Item.IsIdentified's own doc comment. Internal (not private) so
    /// Diagnostics/SelfTest.cs can call it directly -- it never touches Console, so there's
    /// nothing UI-shaped to work around.
    /// </summary>
    internal static string FormatItemDetails(Player player, Item item)
    {
        if (!item.IsIdentified)
        {
            // Known-Information Rule (Item Comparison proposal): mechanical stats stay concealed,
            // but the item's own "mundane" visible quality -- and, when applicable, a hedged
            // comparison against an equipped counterpart -- is knowable without identifying it.
            var unidentifiedLines = new List<string> { item.LongDescription };
            string unidentifiedComparison = ItemComparisonFormatter.FormatLightweightLine(player, item);
            if (unidentifiedComparison != null)
            {
                unidentifiedLines.Add(unidentifiedComparison);
            }
            return string.Join(" | ", unidentifiedLines);
        }

        var details = new List<string> { item.LongDescription };

        if (item.IsBlessed)
        {
            details.Add("Blessed");
        }
        if (item.IsCursed)
        {
            details.Add("Cursed");
        }

        if (item.EquipmentType != EquipmentType.None)
        {
            details.Add($"Slot: {item.EquipmentType}");
        }
        details.Add($"Size: {item.ItemSize.ToDisplayString()}");
        if (item.WeaponType.HasValue)
        {
            details.Add($"Weapon Type: {item.WeaponType}");
        }
        // Launcher compatibility -- Bow/Crossbow/Sling only (RequiredAmmunitionType is null for
        // everything else). Ammo-family/throwable info below covers the other side.
        if (item.RequiredAmmunitionType.HasValue)
        {
            details.Add($"Uses: {item.RequiredAmmunitionType.Value.PluralName()}");
            details.Add($"Range: {item.ProjectileRange}");
        }
        if (item.AmmunitionType.HasValue)
        {
            details.Add($"Family: {item.AmmunitionType}");
            details.Add($"Compatible with: {item.AmmunitionType.Value.LauncherName()}");
        }
        if (item.CanBeThrown)
        {
            details.Add(item.AmmunitionType.HasValue ? "Can also be thrown without a launcher" : "Can be thrown without a launcher");
            details.Add($"Throw Range: {item.ProjectileRange}");
        }
        if (item.ArmorWeight.HasValue)
        {
            details.Add($"Armor Weight: {item.ArmorWeight}");
            int agilityPenalty = EquipmentCompatibility.AgilityPenaltyForWeight(player.Class, item.ArmorWeight);
            if (agilityPenalty > 0)
            {
                details.Add($"Agility: -{agilityPenalty} (a {player.Class.Name} pays this for anything heavier than Light)");
            }
        }
        if (item.PhysicalAttackBonus != 0)
        {
            details.Add($"Physical Attack: +{item.PhysicalAttackBonus}");
        }
        if (item.MagicalAttackBonus != 0)
        {
            details.Add($"Magical Attack: +{item.MagicalAttackBonus}");
        }
        if (item.DefenseBonus != 0)
        {
            details.Add($"Defense: +{item.DefenseBonus}");
        }
        if (item.IsBlessed && item.BlessedUndeadDamageBonus > 0)
        {
            details.Add($"+{item.BlessedUndeadDamageBonus} damage vs. undead");
        }
        if (item.StatusEffects.Count > 0)
        {
            details.Add($"Status Effects: {string.Join(", ", item.StatusEffects.Select(e => e.EffectType.ToString()))}");
        }
        if (item.EncumbranceModifier != 0)
        {
            details.Add($"Carry Capacity: {(item.EncumbranceModifier >= 0 ? "+" : "")}{item.EncumbranceModifier} lbs");
        }
        foreach (var (attribute, amount) in item.StatModifiers)
        {
            details.Add($"{attribute}: {(amount >= 0 ? "+" : "")}{amount}");
        }
        // Resistance System spec sections 22/49 -- one line per non-zero resistance modifier,
        // sign always shown. Negative values (a cursed item's vulnerabilities) are never hidden.
        foreach (var type in Enum.GetValues<ResistanceType>())
        {
            int amount = item.ResistanceModifiers.Get(type);
            if (amount != 0)
            {
                details.Add($"{type} Resistance: {(amount >= 0 ? "+" : "")}{amount}");
            }
        }
        if (item.MinimumLevel > 0)
        {
            details.Add($"Requires Level {item.MinimumLevel}");
        }
        if (item.RequiredClass != null)
        {
            details.Add($"Requires Class: {item.RequiredClass.Name}");
        }
        if (item.RequiredRace != null)
        {
            details.Add($"Requires Race: {item.RequiredRace.Name}");
        }
        if (item.GoldValue > 0)
        {
            details.Add($"Value: {item.GoldValue} gold");
        }

        string comparison = ItemComparisonFormatter.FormatLightweightLine(player, item);
        if (comparison != null)
        {
            details.Add(comparison);
        }

        return string.Join(" | ", details);
    }

    /// <summary>The actual use/equip decision/action, split out from ShowInventoryTab's key handling so it's exercisable without Console.ReadKey -- see Diagnostics/SelfTest.cs, and TraderScreen.TryBuy's identical precedent.</summary>
    internal static string ApplyItem(Player player, Item item, Level level)
    {
        if (!ItemRequirementValidator.CanUse(player, item, out string reason))
        {
            return reason;
        }

        if (item.CastsSpell != null)
        {
            return $"{item.DisplayName} needs a target -- cast it with 'c' instead.";
        }

        if (item.TeachesSpell != null)
        {
            return $"Use the Learn Spell command ('r') to read {item.DisplayName}.";
        }

        if (item.IsIdentifyScroll)
        {
            var candidates = player.Inventory.Items
                .Concat(player.Equipment.AllEquipped.Select(kvp => kvp.Value))
                .Where(i => !i.IsIdentified && i != item)
                .ToList();
            if (candidates.Count == 0)
            {
                return "You have nothing unidentified to identify.";
            }

            var itemLabels = candidates.Select(i => i.DisplayName).ToList();
            int? itemIndex = MenuPrompt.Choose("Identify which item?", itemLabels, allowCancel: true);
            if (itemIndex == null)
            {
                return "Cancelled.";
            }

            var target = candidates[itemIndex.Value];
            target.IsIdentified = true;
            ItemStacking.ConsumeOne(player.Inventory, item);
            return $"You identify the {target.Name}.";
        }

        if (item.Type == ItemType.Key)
        {
            return $"{item.DisplayName} isn't used from here -- walk into a locked door to use it.";
        }

        if (item.Type == ItemType.Lockpick)
        {
            return $"{item.DisplayName} isn't used from here -- it's used automatically whenever you pick a lock.";
        }

        // Light/extinguish toggle (design spec sections 6/7) -- selecting a Candle/Torch/Lantern
        // from here flips IsLit rather than consuming the item, unlike ItemType.Consumable below.
        // Free (no turn consumed), same as every other inventory action; GameLoop's OpenInventory
        // handler recomputes FOV/illumination right after this screen closes so the change shows
        // immediately instead of waiting on the player's next real move.
        if (item.EmitsLight)
        {
            if (item.IsLit)
            {
                item.IsLit = false;
                return $"You extinguish the {item.DisplayName}. ({item.RemainingLightDuration} turns of light remain.)";
            }

            if (item.RemainingLightDuration <= 0)
            {
                return $"The {item.DisplayName} has nothing left to burn.";
            }

            item.IsLit = true;
            return $"You light the {item.DisplayName}.";
        }

        if (item.Type == ItemType.Consumable)
        {
            player.Health.Heal(item.HealthRestore);
            player.Mana.Restore(item.ManaRestore);

            if (item.CarryCapacityBonus != 0)
            {
                player.ActiveEffects.Add(new ActiveEffect(item.DisplayName, level.TurnNumber + item.CarryCapacityBonusDuration)
                {
                    ModifiedStat = Stat.CarryCapacity,
                    StatAmount = item.CarryCapacityBonus
                });
                StatModifierEffect.ApplyStatDelta(player, Stat.CarryCapacity, item.CarryCapacityBonus);
            }

            ItemStacking.ConsumeOne(player.Inventory, item);
            return $"You use the {item.DisplayName}.";
        }

        if (item.EquipmentType == EquipmentType.None)
        {
            return $"{item.DisplayName} can't be equipped.";
        }

        var slot = player.Equipment.FindAutoEquipSlot(item);
        if (slot == null)
        {
            // Only reachable for Hand/Ring items when both compatible slots are already occupied.
            slot = PromptReplaceSlot(EquipmentCompatibility.GetCompatibleSlots(item));
            if (slot == null)
            {
                return "Cancelled.";
            }
        }

        // Readying more of what's already equipped in a stackable slot (Ammo/Thrown) merges into
        // the existing readied stack instead of swapping it out to inventory and back -- the same
        // outcome ItemStacking already gives an ordinary carried stack, just for the equipped
        // copy too. Every other equipment type is never IsStackable, so this never applies to them.
        var alreadyEquipped = player.Equipment.Get(slot.Value);
        if (alreadyEquipped != null && ItemStacking.CanStackTogether(alreadyEquipped, item))
        {
            alreadyEquipped.Charges = (alreadyEquipped.Charges ?? 1) + (item.Charges ?? 1);
            player.Inventory.RemoveItem(item);
            return $"You add {item.DisplayName} to your readied stack ({alreadyEquipped.Charges} total).";
        }

        if (!EquipmentCompatibility.CanEquip(player, item, slot.Value, out string equipFailureReason))
        {
            return equipFailureReason;
        }

        var previous = player.EquipInSlot(slot.Value, item);
        player.Inventory.RemoveItem(item);
        if (previous != null)
        {
            player.Inventory.AddItem(previous);
        }
        return $"You equip the {item.DisplayName} ({EquipmentCompatibility.SlotLabel(slot.Value)}).";
    }

    private static EquipmentSlot? PromptReplaceSlot(IReadOnlyList<EquipmentSlot> options)
    {
        var labels = options.Select(EquipmentCompatibility.SlotLabel).ToList();
        int? index = MenuPrompt.Choose("Both slots are full -- replace which?", labels, allowCancel: true);
        return index.HasValue ? options[index.Value] : null;
    }
}
