namespace BENEATH_FORGOTTEN_STONE.Core.Screens;

/// <summary>
/// Help screen displayed when the player presses 'H' while exploring. Built as padded rows (not
/// a hand-typed block of text) so columns line up correctly by construction -- the game defaults
/// to a 60-column console, so hand-counted spacing is easy to get subtly wrong. Paginated rather
/// than printed as one block: this is reference content that only grows as commands are added, so
/// a fixed page size scales indefinitely where trying to keep compacting it forever eventually
/// can't win. Commands and floor tile types are shown as two independently paginated sections
/// (ShowCommands then ShowFloorTypes) rather than one combined list, so the floor-type reference
/// always starts on its own fresh page instead of wherever the command list happens to run out.
/// </summary>
public static class HelpScreen
{
    /// <summary>Rows reserved below the content for the page footer.</summary>
    private const int FooterLines = 2;

    public static void Show()
    {
        ShowPaginated(BuildCommandLines());
        ShowPaginated(BuildFloorTypeLines());

        // Renderer.Render only redraws the map's own footprint -- this screen's
        // wider/taller text would otherwise leave stray characters behind once
        // the game view resumes.
        ConsoleSafety.TryClear();
    }

    private static List<string> BuildCommandLines() => new()
    {
        "=== Help ===",
        "",
        "MOVEMENT (while exploring) -- numpad only",
        Rule(),
        Row2("NumPad 7 / 8 / 9", "Move northwest / north / northeast"),
        Row2("NumPad 4 / 6", "Move west / east"),
        Row2("NumPad 1 / 2 / 3", "Move southwest / south / southeast"),
        "  If NumLock is off (or your terminal never sends NumPad",
        "  key codes), the same physical keys still work: Home/",
        "  Up/PageUp/Left/Right/End/Down/PageDown for the 8",
        "  directions, matching the numpad's own layout.",
        "  (walking into a creature attacks it, instead of moving --",
        "  walking into a locked door offers a way through instead)",
        "",
        "ACTIONS (while exploring)",
        Rule(),
        "  (any menu that asks you to pick something -- an item,",
        "  a spell, a slot -- can be backed out of with Esc)",
        Row3("Space", "Wait", "Pass your turn"),
        Row3("NumPad5 / 5 / :", "Look Here", "Summarize your own tile --"),
        Row3("", "", "hazards/darkness and items here"),
        Row3("G", "Pick Up", "Pick up an item on your tile --"),
        Row3("", "", "asks which if more than one; a"),
        Row3("", "", "wearable item may offer to equip"),
        Row3("", "", "it into an empty slot (not Get All)"),
        Row3("D", "Drop", "Drop a carried item onto your"),
        Row3("", "", "tile -- unequip it first if worn"),
        Row3("", "", "Very Small items may be lost;"),
        Row3("", "", "larger bundles are safer"),
        Row3("I", "Inventory", "Open your character sheet"),
        Row3("C", "Cast", "Pick a known spell or charged"),
        Row3("", "", "item (like a wand), then aim"),
        Row3("L", "Look", "Aim a direction; inspect the"),
        Row3("", "", "closest visible creature/item"),
        Row3("", "", "along it -- full detail if"),
        Row3("", "", "adjacent, brief if farther away"),
        Row3("R", "Learn Spell", "Read a scroll or spellbook from"),
        Row3("", "", "inventory to permanently learn"),
        Row3("", "", "its spell -- spellcasters only"),
        Row3("S", "Use Skill", "Pick a known physical skill,"),
        Row3("", "", "then aim -- Warrior/Thief only;"),
        Row3("", "", "passives are always-on instead"),
        Row3("O", "Open Chest", "Open/close a chest on your tile --"),
        Row3("", "", "free if unlocked, otherwise"),
        Row3("", "", "requires Pick Lock + lockpicks."),
        Row3("", "", "Stays in the world either way."),
        Row3("F", "Fire/Throw", "Fire or throw whatever's readied"),
        Row3("", "", "in your Ammo/Thrown slot; aim after."),
        Row3("", "", "Ammo needs a compatible Ranged"),
        Row3("", "", "Weapon equipped; a dedicated"),
        Row3("", "", "throwable never needs one. Small"),
        Row3("", "", "items may be lost or break,"),
        Row3("", "", "especially on a miss or in the dark"),
        Row3("P", "Push", "Aim a direction; push a movable"),
        Row3("", "", "room object (boulder, statue) one"),
        Row3("", "", "tile further away from you"),
        Row3(">", "Descend", "Go down -- only works standing"),
        Row3("", "", "on a down staircase"),
        Row3("<", "Ascend", "Go up -- only works standing"),
        Row3("", "", "on an up staircase"),
        Row3("M", "Message History", "Show every recent message, not"),
        Row3("", "", "just what fits on screen"),
        Row3("A", "Adventure Record", "Show statistics for the current"),
        Row3("", "", "run"),
        Row3("X", "Search", "Examine your tile and the 8"),
        Row3("", "", "around it for misplaced items --"),
        Row3("", "", "always costs a turn, even if"),
        Row3("", "", "nothing is found"),
        Row3("W", "Sleep", "Rest until fully healed --"),
        Row3("", "", "blocked while poisoned/burning,"),
        Row3("", "", "on Fire/Lava, or already full."),
        Row3("", "", "Any key wakes you (Esc always"),
        Row3("", "", "does); H opens Help instead."),
        Row3("", "", "Risks an ambush, worse at low Luck"),
        Row3("T", "Stand", "Get back on your feet while prone --"),
        Row3("", "", "always costs a turn. While prone,"),
        Row3("", "", "only Stand and the read-only screens"),
        Row3("", "", "below work; everything else is"),
        Row3("", "", "rejected for free"),
        Row3("U", "Swap w/ Pet", "Trade places with your pet outright"),
        Row3("", "", "if it's adjacent -- unlike bumping"),
        Row3("", "", "into it, this always swaps exactly,"),
        Row3("", "", "never shoves it somewhere random"),
        Row3("H", "Help", "Show this screen"),
        Row3("Q", "Quit", "Save your progress and exit"),
        "",
        "  Y, B, N, and K aren't bound to anything right",
        "  now -- reserved for future commands.",
        "",
        "CHARACTER SHEET (opened with I)",
        Rule(),
        "  Left/Right switch between the Character / Inventory /",
        "  Spells & Skills views; U, L, and D work from any of them.",
        Row3("1-9, a-z", "Select", "Inventory view only -- use a"),
        Row3("", "", "consumable, or equip the item;"),
        Row3("", "", "selecting a Candle/Torch/Lantern"),
        Row3("", "", "lights it (or puts it out if lit)"),
        Row3("U", "Unequip", "Choose a slot to empty; the"),
        Row3("", "", "item returns to your inventory."),
        Row3("", "", "Equipping/replacing/removing Ranged"),
        Row3("", "", "Weapon or Ammo/Thrown costs a turn --"),
        Row3("", "", "every other slot stays free"),
        Row3("L", "Examine", "Choose any carried/equipped"),
        Row3("", "", "item; shows the full detail"),
        Row3("", "", "with no direction/distance --"),
        Row3("", "", "unlike Look while exploring"),
        Row3("D", "Drop", "Choose a carried item to drop"),
        Row3("Esc", "Close", "Return to the game"),
    };

    /// <summary>Player-facing summary of every FloorType -- see Dungeon/FloorTypeDefinition for the authoritative numbers this reflects. Deliberately doesn't mention the tile-attuned monster system's own per-monster off-terrain attrition percentages/path costs; those are implementation tuning, not something a player needs memorized to play around the visible pattern described here.</summary>
    private static List<string> BuildFloorTypeLines() => new()
    {
        "=== Floor Tile Types ===",
        "",
        "  Special floor tiles sometimes appear in clusters within a",
        "  room. All of them are walkable -- some just hurt to stand",
        "  on, or change how much elemental damage you take there.",
        "",
        Row3(".", "Normal", "Plain dungeon floor. No effect."),
        Row3("~", "Water (blue)", "Halves Fire damage taken here;"),
        Row3("", "", "increases Lightning damage 1.5x"),
        Row3("=", "Ice (cyan)", "Icy footing. No damage modifier"),
        Row3("", "", "of its own."),
        Row3("^", "Fire (red)", "Burns you for a few HP every"),
        Row3("", "", "turn you stand on it (scales"),
        Row3("", "", "with dungeon depth); increases"),
        Row3("", "", "Fire damage taken 1.5x, halves"),
        Row3("", "", "Water/Ice damage taken"),
        Row3("~", "Lava (dark red)", "Same glyph as Water, colored"),
        Row3("", "", "differently -- burns for much"),
        Row3("", "", "more than Fire per turn; same"),
        Row3("", "", "Fire/Water/Ice modifiers as Fire"),
        Row3(":", "Mud (dark yellow)", "No damage modifier of its own."),
        Row3(".", "Sand (yellow)", "No damage modifier of its own."),
        Row3("\"", "Grass (green)", "No damage modifier of its own."),
        Row3(",", "Swamp (dark green)", "No damage modifier of its own."),
        Row3(":", "Ash (gray)", "No damage modifier of its own."),
        "",
        "  Some creatures are naturally attuned to one of these",
        "  tiles (e.g. a Water Imp to Water) -- on their own terrain",
        "  they resist their opposing element and take no harm from",
        "  lingering there; pulled away from it, they slowly weaken",
        "  and become more vulnerable to that element instead.",
        "",
        "DARK ROOMS",
        Rule(),
        "  Some rooms are naturally dark -- unlit portions render as",
        "  solid black, hiding terrain, items, and monsters until a",
        "  light source reaches them (you always see yourself). A",
        "  lit Candle/Torch/Lantern, or a Mage's Arcane Orb / Priest's",
        "  Divine Radiance, illuminates a radius around its carrier;",
        "  walls still block it like any line of sight. Water",
        "  extinguishes a lit physical light on contact; walking onto",
        "  an unseen item in the dark has a chance to notice (not",
        "  identify) it, and an attack from an unlit tile won't name",
        "  its source.",
    };

    private static void ShowPaginated(List<string> lines)
    {
        int pageSize = GetPageSize();
        int pageCount = (int)Math.Ceiling(lines.Count / (double)pageSize);

        for (int page = 0; page < pageCount; page++)
        {
            var pageLines = lines.Skip(page * pageSize).Take(pageSize).ToList();
            pageLines.Add("");
            pageLines.Add(page < pageCount - 1
                ? $"-- Page {page + 1}/{pageCount}: press any key for more --"
                : $"-- Page {page + 1}/{pageCount}: press any key to continue --");

            Renderer.RenderMessage(string.Join('\n', pageLines));
            Console.ReadKey(true);
        }
    }

    private static int GetPageSize()
    {
        try
        {
            return Math.Max(10, Console.WindowHeight - FooterLines - 1);
        }
        catch (IOException)
        {
            return 25;
        }
    }

    private static string Rule() => new('-', 50);

    private static string Row2(string key, string description) => $"  {key,-27}{description}";

    private static string Row3(string key, string name, string description) => $"  {key,-17}{name,-19}{description}";
}
