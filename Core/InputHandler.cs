namespace BENEATH_FORGOTTEN_STONE.Core;

public enum PlayerCommand
{
    None,
    MoveNorth,
    MoveSouth,
    MoveEast,
    MoveWest,
    MoveNorthEast,
    MoveNorthWest,
    MoveSouthEast,
    MoveSouthWest,
    Wait,
    PickUp,
    OpenInventory,
    ShowHelp,
    CastSpell,
    Look,
    LearnSpell,
    DropItem,
    UseSkill,
    OpenChest,
    FireProjectile,
    AscendStairs,
    DescendStairs,
    ShowMessageHistory,
    Push,
    LookHere,
    ShowAdventureRecord,
    Search,
    Sleep,
    Stand,
    SwapWithPet,
    Quit
}

/// <summary>
/// Movement is the numpad (8/2/4/6 cardinal, 7/9/1/3 diagonal) -- arrow keys as their
/// own separate binding, WASD, and the old vi-style Y/U/B/N diagonals are deliberately NOT
/// accepted, freeing those letters up. Two of them were reused for a clearer mnemonic than
/// their old letter (D now means Drop instead of P, S now means Skill instead of K -- both were
/// only ever on P/K because D and S were themselves movement keys before); P has since been
/// claimed by Push (Room Objects spec), A by ShowAdventureRecord, W by Sleep, and U by
/// SwapWithPet (Pet and Companion System -- no stronger mnemonic than "it was free," same as
/// X's own claim for Search below). Y, B, N, and K remain intentionally unbound, reserved for
/// future commands rather than assigned just to fill space.
///
/// Each direction also accepts the key a numpad digit sends with NumLock OFF (Home/End/PageUp/
/// PageDown for the diagonals, the plain arrow keys for the 4 cardinals) -- not a reintroduction
/// of separate arrow-key movement, but numpad support: on many terminals (Windows Terminal/
/// ConPTY chief among them) Console.ReadKey never reliably reports ConsoleKey.NumPad8 etc. at
/// all regardless of NumLock state, only the navigation-key identity the physical numpad key
/// sends when unlocked. Accepting both is what actually makes "press the numpad" work in
/// practice across terminals, rather than only in the one config where NumPad-coded keys happen
/// to come through.
///
/// Stairs are explicit commands ('&lt;' to ascend, '&gt;' to descend) rather than auto-triggered
/// by stepping onto the tile -- see GameLoop.HandleUseStairs. 'm' opens the message history.
///
/// 'W' is Sleep (see GameLoop.HandleStartSleeping) -- one of the letters this comment used to
/// call out as intentionally unbound/reserved for a future command.
///
/// NumPad5 (and its NumLock-off equivalent, Clear -- see the numpad-support paragraph above) is
/// LookHere, not Wait: it summarizes the player's own current tile instead of passing a turn.
/// Spacebar is now the only way to wait. See GameLoop.HandleLookHere. Unlike the other 8 numpad
/// keys, the center key has no navigation-key identity to fall back on when a terminal doesn't
/// forward NumPad5/Clear with NumLock off -- D5 (the plain '5' digit) is the guaranteed-working
/// alternate for LookHere specifically, for that reason.
/// </summary>
public static class InputHandler
{
    public static PlayerCommand ReadCommand() => ResolveCommand(Console.ReadKey(intercept: true));

    /// <summary>
    /// The actual key-to-command mapping, split out from ReadCommand's own blocking Console.ReadKey
    /// call so GameLoop's Sleep command can feed a non-blockingly-peeked key (see
    /// ConsoleSafety.TryReadKeyIfAvailable) through the exact same resolution logic without a
    /// second blocking read.
    /// </summary>
    public static PlayerCommand ResolveCommand(ConsoleKeyInfo keyInfo)
    {
        // '<'/'>' are Shift+comma/period on every layout this game ships to, and Console.ReadKey's
        // ConsoleKey alone can't distinguish shifted from unshifted (both report OemComma/
        // OemPeriod) -- checking KeyChar directly sidesteps needing to also inspect Modifiers.
        if (keyInfo.KeyChar == '<')
        {
            return PlayerCommand.AscendStairs;
        }
        if (keyInfo.KeyChar == '>')
        {
            return PlayerCommand.DescendStairs;
        }
        // ':' is the traditional roguelike "what's here" binding (NetHack et al.) -- added as a
        // fully keyboard-layout/NumLock-independent alternative for LookHere after NumPad5/Clear/D5
        // all turned out to be unreliable on some terminals with NumLock off (see the class doc
        // comment above). Checked via KeyChar for the same reason '<'/'>' are.
        if (keyInfo.KeyChar == ':')
        {
            return PlayerCommand.LookHere;
        }

        return keyInfo.Key switch
        {
            ConsoleKey.NumPad8 or ConsoleKey.UpArrow => PlayerCommand.MoveNorth,
            ConsoleKey.NumPad2 or ConsoleKey.DownArrow => PlayerCommand.MoveSouth,
            ConsoleKey.NumPad6 or ConsoleKey.RightArrow => PlayerCommand.MoveEast,
            ConsoleKey.NumPad4 or ConsoleKey.LeftArrow => PlayerCommand.MoveWest,
            ConsoleKey.NumPad7 or ConsoleKey.Home => PlayerCommand.MoveNorthWest,
            ConsoleKey.NumPad9 or ConsoleKey.PageUp => PlayerCommand.MoveNorthEast,
            ConsoleKey.NumPad1 or ConsoleKey.End => PlayerCommand.MoveSouthWest,
            ConsoleKey.NumPad3 or ConsoleKey.PageDown => PlayerCommand.MoveSouthEast,
            ConsoleKey.Spacebar => PlayerCommand.Wait,
            // NumPad5's NumLock-off identity is Clear/Begin -- unlike the other 8 numpad keys
            // (which fall back to a real navigation key: Home/End/PageUp/PageDown/arrows), the
            // center key has no such fallback and many terminals (Windows Terminal/ConPTY
            // included) simply never send anything recognizable for it with NumLock off. D5 (the
            // physical top-row/keypad '5' digit, same mnemonic) is the reliable fallback.
            ConsoleKey.NumPad5 or ConsoleKey.Clear or ConsoleKey.D5 => PlayerCommand.LookHere,
            ConsoleKey.G => PlayerCommand.PickUp,
            ConsoleKey.I => PlayerCommand.OpenInventory,
            ConsoleKey.C => PlayerCommand.CastSpell,
            ConsoleKey.L => PlayerCommand.Look,
            ConsoleKey.R => PlayerCommand.LearnSpell,
            // D = Drop -- moved off P now that D is free (no longer Move East).
            ConsoleKey.D => PlayerCommand.DropItem,
            // S = Skill -- moved off K now that S is free (no longer Move South).
            ConsoleKey.S => PlayerCommand.UseSkill,
            ConsoleKey.O => PlayerCommand.OpenChest,
            ConsoleKey.F => PlayerCommand.FireProjectile,
            ConsoleKey.H => PlayerCommand.ShowHelp,
            ConsoleKey.M => PlayerCommand.ShowMessageHistory,
            // P = Push (Room Objects spec) -- one of the letters explicitly left unbound and
            // reserved for a future command, per this class's own doc comment above.
            ConsoleKey.P => PlayerCommand.Push,
            // A = Adventure Record -- one of the letters explicitly left unbound and reserved
            // for a future command, per this class's own doc comment above.
            ConsoleKey.A => PlayerCommand.ShowAdventureRecord,
            // X = Search (Misplaced Items design doc) -- examines the player's tile and its
            // 8 neighbors for concealed ground items. Was free/unbound before this.
            ConsoleKey.X => PlayerCommand.Search,
            // W = Sleep (Sleep/Rest proposal) -- one of the letters explicitly left unbound and
            // reserved for a future command, per this class's own doc comment above.
            ConsoleKey.W => PlayerCommand.Sleep,
            // T = sTand (Prone/Knockdown System) -- "S" was already taken by Use Skill, same
            // reasoning the class doc comment gives for D/S's own mnemonic reassignments.
            ConsoleKey.T => PlayerCommand.Stand,
            // U = swap places with your pet (Pet and Companion System) -- distinct from bumping
            // into it (which shoves the pet aside at random): a deliberate, direction-independent
            // command that trades tiles with an adjacent pet outright.
            ConsoleKey.U => PlayerCommand.SwapWithPet,
            ConsoleKey.Q => PlayerCommand.Quit,
            _ => PlayerCommand.None
        };
    }

    public static (int Dx, int Dy) ToDelta(PlayerCommand command) => command switch
    {
        PlayerCommand.MoveNorth => (0, -1),
        PlayerCommand.MoveSouth => (0, 1),
        PlayerCommand.MoveEast => (1, 0),
        PlayerCommand.MoveWest => (-1, 0),
        PlayerCommand.MoveNorthEast => (1, -1),
        PlayerCommand.MoveNorthWest => (-1, -1),
        PlayerCommand.MoveSouthEast => (1, 1),
        PlayerCommand.MoveSouthWest => (-1, 1),
        _ => (0, 0)
    };
}
