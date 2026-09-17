using BENEATH_FORGOTTEN_STONE.Core.Screens;
using BENEATH_FORGOTTEN_STONE.Dungeon;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.AI;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Proficiency;
using BENEATH_FORGOTTEN_STONE.Entities.Projectiles;
using BENEATH_FORGOTTEN_STONE.Entities.Skills;
using BENEATH_FORGOTTEN_STONE.Entities.Spells;
using BENEATH_FORGOTTEN_STONE.Persistence;

namespace BENEATH_FORGOTTEN_STONE.Core;

/// <summary>
/// Ties together the Dungeon Manager, Turn Scheduler, Input Handler and
/// Renderer into the actual game/turn loop.
/// </summary>
public class GameLoop
{
    private const int ViewRadius = 8;

    private readonly DungeonManager dungeonManager;
    private readonly Player player;
    private readonly Random rng = new();

    private bool isRunning = true;

    /// <summary>
    /// Wall-clock moment THIS session began -- the character-creation moment for a fresh
    /// character, or the moment a save was loaded for a resumed one, since GameLoop is
    /// constructed exactly once at either point. See RecordElapsedDungeonTime, which folds the
    /// time elapsed since this into Player.AdventureRecord.TimeInDungeonSeconds and resets this
    /// back to "now" -- called on quit (before saving), on death, and whenever the Adventure
    /// Record screen is opened mid-run, so a live check always shows an up-to-date total instead
    /// of only ever reflecting previously-completed sessions.
    /// </summary>
    private DateTime sessionStartUtc = DateTime.UtcNow;

    /// <summary>Every status/flavor/combat line the player has seen this session, oldest first -- see MessageLog. Renderer only ever draws the most recent MessageLog.VisibleLines of these each turn; 'm' (MessageHistoryScreen) shows the full retained history.</summary>
    private readonly MessageLog messageLog = new();

    /// <summary>Whether the player was standing on an unilluminated dark tile as of the last completed turn -- compared against the fresh value every turn to fire "Darkness closes around you."/"The darkness retreats from the light." exactly on a meaningful change (design spec section 11), regardless of whether movement, a light going out, or a light being lit caused it.</summary>
    private bool playerInDarkness;

    /// <summary>Sleep command state -- see HandleStartSleeping/CheckSleepInterrupts/HandleSleepInputKey. All reset to their defaults whenever sleep ends, for any reason.</summary>
    private bool isSleeping;

    /// <summary>The turn a rolled ambush actually arrives, if HandleStartSleeping's initial roll succeeded -- null means no ambush is scheduled this sleep.</summary>
    private int? sleepAmbushTurn;

    private int sleepHpAtStart;
    private int sleepManaAtStart;

    /// <summary>HP as of the last completed CheckSleepInterrupts call -- a drop means something (an existing monster's attack, environmental damage) already printed its own message this tick; see CheckSleepInterrupts.</summary>
    private int sleepHpSnapshot;

    /// <summary>Whether the player had a harmful (TickDamage &gt; 0) ActiveEffect as of the last check -- a false-to-true transition means one was just newly applied.</summary>
    private bool sleepHadHarmfulEffect;

    /// <summary>Whether a hostile Monster was already adjacent as of the last check -- a false-to-true transition wakes the player proactively (the design doc's "otherwise endangered"), before that monster's own attack would land.</summary>
    private bool sleepHadAdjacentMonster;

    /// <summary>Test-only window into the current floor and status-message history -- see Diagnostics/SelfTest.cs.</summary>
    internal Level CurrentLevel => dungeonManager.CurrentLevel;
    internal IReadOnlyList<string> StatusMessages => messageLog.History.Select(e => e.Text).ToList();

    /// <summary>Test-only seam for the Ability Proficiency System's yellow lucky-insight/rank-up/Mastery messages -- see Diagnostics/SelfTest.cs.</summary>
    internal ConsoleColor LastStatusMessageColor => messageLog.History[^1].Color;

    public GameLoop(Player player) : this(player, resumeFloorIndex: null, restoredFloors: null)
    {
    }

    /// <summary>
    /// restoredFloors seeds DungeonManager with every floor reconstructed from a save
    /// (see SaveManager.ToLevels) before entering resumeFloorIndex, so that floor is used
    /// exactly as left instead of generated fresh. The player's saved (X, Y) -- already set
    /// by SaveManager.ToPlayer at this point -- is captured before EnterFloor's placement
    /// logic overwrites it, so a resumed game drops the player back where they saved rather
    /// than at the stairs. levelsSinceLastTrader restores the cross-floor trader-frequency
    /// counter (see DungeonManager.LevelsSinceLastTrader) so resuming doesn't reset the
    /// guarantee's progress.
    /// </summary>
    public GameLoop(Player player, int? resumeFloorIndex, IReadOnlyDictionary<int, Level> restoredFloors = null, int levelsSinceLastTrader = 0)
    {
        this.player = player;
        dungeonManager = new DungeonManager(rng);
        dungeonManager.RestoreTraderCounter(levelsSinceLastTrader);

        if (restoredFloors != null)
        {
            foreach (var (index, level) in restoredFloors)
            {
                dungeonManager.RestoreFloor(index, level);
            }
        }

        if (resumeFloorIndex.HasValue)
        {
            var resumePosition = (player.X, player.Y);
            dungeonManager.EnterFloor(player, resumeFloorIndex.Value, resumePosition);
        }
        else
        {
            dungeonManager.EnterFirstFloor(player);
        }

        PlacePetIfNeeded();
        RecomputeFov();
        playerInDarkness = IsPlayerInDarkness(dungeonManager.CurrentLevel, player); // silent -- nothing "changed" at game start
        messageLog.Add("Welcome! Numpad 1-9 to move (space to wait, 5 to look at your own tile), bump to attack, i for stats, c to cast, s for skills, l to look, p to push, r to learn spells, d to drop, o to open chests, f to fire/throw, > and < for stairs, m for message history, a for your adventure record, w to sleep, t to stand if knocked prone, u to swap places with your pet, h for help, q to save & quit.");
    }

    public void Run()
    {
        // CursorVisible/console sizing need a real console handle; both throw
        // when stdout isn't backed by one (e.g. VS Code's internalConsole debugger).
        ConsoleSafety.TrySetCursorVisible(false);
        // +1 blank spacer row, +1 stats row, +floor-item rows, +message-history rows.
        ConsoleSafety.TryFitConsole(
            dungeonManager.CurrentLevel.Width,
            dungeonManager.CurrentLevel.Height + 2 + Renderer.MaxFloorItemLines + Renderer.MaxStatusMessageLines);
        ConsoleSafety.TryClear();

        while (isRunning)
        {
            var level = dungeonManager.CurrentLevel;

            // Sleep command: checked every tick (not just the player's own), so a keypress
            // arriving while a monster is mid-turn is still noticed immediately -- see
            // HandleSleepInputKey. A key that wakes the player skips this tick's actor turn
            // entirely rather than letting a stale GetNextActor() result play out.
            if (isSleeping)
            {
                var pendingKey = ConsoleSafety.TryReadKeyIfAvailable();
                if (pendingKey.HasValue)
                {
                    HandleSleepInputKey(pendingKey.Value);
                    if (!isSleeping)
                    {
                        continue;
                    }
                }
            }

            var actor = level.Scheduler.GetNextActor();
            if (actor == null)
            {
                // Should never happen -- the player is always registered -- but don't hang if it does.
                continue;
            }

            // A monster's turn that the player can't currently see can never change what's on
            // screen, so redrawing for it is pure waste -- and on a floor with many freshly
            // spawned monsters (all starting at 0 energy, so many cross the ready threshold in
            // the same synchronized batch -- see TurnScheduler), skipping it is the difference
            // between one full-screen redraw per visible action and dozens of console writes
            // stacking up back-to-back before the player ever gets a turn back, which is
            // expensive enough on a real console to look and feel like the game has frozen.
            // Nothing about status-message content is lost either way -- it's just shown the
            // next time something IS rendered instead of immediately.
            if (ShouldRenderTurn(level, actor))
            {
                Renderer.Render(level, player, messageLog.GetRecent(MessageLog.VisibleLines), blackedOut: isSleeping);
            }

            bool turnTaken;
            if (actor is Player)
            {
                turnTaken = ProcessPlayerTurn(level);
            }
            else
            {
                string aiMessage = ResolveNonPlayerTurn(actor, level, rng);
                if (!string.IsNullOrEmpty(aiMessage))
                {
                    AddStatusMessage(aiMessage);
                }
                turnTaken = true;
            }

            if (turnTaken)
            {
                level.Scheduler.ConsumeEnergy(actor);
            }

            // Intercession (New Priest Skill Progression): CombatStatsTracker.ApplyDamage has no
            // messageLog reference of its own, so it leaves its flavor text here for the very next
            // opportunity after whichever actor's turn caused the damage -- melee, a trap, a DoT
            // tick, environmental Fire/Lava, all funnel through that one method regardless of
            // which actor's turn just resolved, so checking this once per completed turn (not just
            // the player's own) catches every case.
            if (player.PendingInterceptionMessage != null)
            {
                AddStatusMessage(player.PendingInterceptionMessage, ConsoleColor.Yellow);
                player.PendingInterceptionMessage = null;
            }

            // Pet and Companion System: mirrors PendingInterceptionMessage's own flush pattern --
            // PetFactory's OnLevelUp subscription has no messageLog reference of its own.
            if (player.PendingPetMessage != null)
            {
                AddStatusMessage(player.PendingPetMessage);
                player.PendingPetMessage = null;
            }

            // Checked after ANY actor's turn (not just the player's own automatic sleep-wait) --
            // "attacked by an existing monster" can land on a monster's own scheduled turn while
            // the player sleeps, same as any other turn-based system continuing normally.
            if (isSleeping)
            {
                CheckSleepInterrupts(level);
            }

            if (actor is Player && turnTaken)
            {
                // Checked BEFORE AdvanceTurn below, against the turn number every LastAttackTurn
                // this round was actually stamped with -- "this turn" is a diff of 0, "last turn"
                // a diff of 1, matching IsPlayerInCombat's own <= 1 check exactly. Checking after
                // the increment would shift both by one and invite an off-by-one bug here.
                bool inCombat = IsPlayerInCombat(level, player);

                level.AdvanceTurn();
                player.TurnCount++;

                // Intercession's protection window expires harmlessly once its duration runs out
                // without ever facing a lethal hit -- mirrors the plain turn-deadline pattern
                // Stunned/Silenced/CcImmune already use, just checked once per completed turn
                // instead of read on demand (Intercession has no "is this currently blocking an
                // action" question the way those three do).
                if (player.IntercessionActive && level.TurnNumber >= player.IntercessionExpiresOnTurn)
                {
                    player.IntercessionActive = false;
                }

                // Pet and Companion System: the 20-completed-owner-turn respawn delay is checked
                // here, the same "one completed normal player turn" spot TurnCount++ above just
                // ran -- see Pet.RespawnAtOwnerTurn's own doc comment for why this must be the
                // owner's global turn count, not any per-level Level.TurnNumber.
                if (player.Pet is { LifecycleState: PetLifecycleState.AwaitingRespawn } pet && player.TurnCount >= pet.RespawnAtOwnerTurn)
                {
                    RespawnPet(pet);
                }

                // Environmental floor damage (Fire/Lava) -- once per completed normal turn,
                // from this same centralized spot, so standing on Fire and moving vs. WAITing
                // both apply damage exactly once, never twice and never zero. Ordered before
                // regeneration below so a floor that would otherwise be fatal this turn still
                // gets normal death processing (player.IsAlive) before regen ever runs.
                string environmentalMessage = EnvironmentalFloorEffects.Apply(level, player);
                if (environmentalMessage != null)
                {
                    AddStatusMessage(environmentalMessage);
                }

                // Same Fire/Lava tile damage as the player just took, generalized to every
                // monster (design spec section 10) and pet (Pet and Companion System), plus the
                // tile-attuned monster system's own off-preferred-floor attrition (sections 6-9)
                // -- both run from this same centralized once-per-completed-turn spot rather than
                // per monster-action, the same reasoning EffectProcessor.Tick below already
                // applies to every actor's status-effect ticks.
                EnvironmentalFloorEffects.ApplyToNonPlayerActors(level);
                foreach (var attunementMessage in EnvironmentalFloorEffects.ApplyFloorAttunementAttrition(level))
                {
                    AddStatusMessage(attunementMessage);
                }

                // Natural regeneration runs here -- the one centralized "the player just
                // consumed a normal turn" spot, alongside TurnCount above -- rather than inside
                // any individual command, so a projectile's several internal tiles or a
                // multi-step menu never trigger it more than once per real player action.
                // Ordered before EffectProcessor.Tick's status-effect damage (matches the
                // reference spec's own recommended order) so a DoT that would otherwise be
                // fatal this turn still gets a chance to be offset by regen first, not after.
                if (player.IsAlive)
                {
                    ResourceRegenerationCalculator.ApplyRegen(player, inCombat, restMultiplier: isSleeping ? RegenerationConfig.SleepRegenMultiplier : 1.0);
                }

                foreach (var effectMessage in EffectProcessor.Tick(level))
                {
                    AddStatusMessage(effectMessage);
                }

                // Ambient sound -- purely atmospheric, from this same centralized "one
                // completed normal player turn" spot so a fired/thrown projectile's own
                // multi-tile travel never triggers extra checks (see SoundSystem's own doc
                // comment). A dead player has nothing left to hear.
                if (player.IsAlive)
                {
                    string soundMessage = SoundSystem.ProcessTurn(level, player, rng);
                    if (soundMessage != null)
                    {
                        AddStatusMessage(soundMessage);
                    }
                }

                // Physical light burn/random-extinguish, from this same centralized spot -- a
                // lit torch burns down on a WAIT the same as it does on a move. Illumination is
                // recomputed again right after (a light dying out here, or a light spell expiring
                // in EffectProcessor.Tick above, both need reflecting immediately, not just on
                // the next move-triggered RecomputeFov), and the darkness-transition check reads
                // the result of THAT recompute, not the movement-time one from earlier this turn.
                if (player.IsAlive)
                {
                    foreach (var lightMessage in LightingSystem.ProcessTurn(player, rng))
                    {
                        AddStatusMessage(lightMessage);
                    }

                    LightingSystem.RecomputeIllumination(level);
                    FieldOfView.Compute(level, player.X, player.Y, ViewRadius);

                    bool nowInDarkness = IsPlayerInDarkness(level, player);
                    if (nowInDarkness != playerInDarkness)
                    {
                        AddStatusMessage(nowInDarkness ? "Darkness closes around you." : "The darkness retreats from the light.");
                        playerInDarkness = nowInDarkness;
                    }
                }
            }

            AwardDeathRewards(level);
            HandlePetDeath(level);
            level.RemoveDeadActors();

            if (!isRunning)
            {
                break;
            }

            if (!player.IsAlive)
            {
                HandleDeath();
                break;
            }
        }
    }

    /// <summary>
    /// Always render for the player's own turn (they're about to make a decision from it);
    /// for a monster, only if it's standing on a tile the player can currently see -- an
    /// off-screen monster's turn can't change anything visible, so redrawing for it would
    /// just be an expensive no-op. Internal (not private) so Diagnostics/SelfTest.cs can
    /// verify the decision directly.
    /// </summary>
    internal static bool ShouldRenderTurn(Level level, Actor actor) =>
        actor is Player || (level.IsInBounds(actor.X, actor.Y) && level.Tiles[actor.X, actor.Y].IsVisible);

    /// <summary>
    /// A non-player actor's own turn: stunned (loses the turn outright, still consumed by
    /// ConsumeEnergy back in Run) takes priority over prone (Prone/Knockdown System -- always
    /// spends the turn standing back up instead of running AI), which takes priority over normal
    /// AI. Extracted from Run's own loop so Diagnostics/SelfTest.cs can exercise each branch
    /// directly instead of driving the whole turn loop (which reads player input via Console).
    /// </summary>
    internal static string ResolveNonPlayerTurn(Actor actor, Level level, Random rng)
    {
        if (level.TurnNumber < actor.StunnedUntilTurn)
        {
            // Bash landed on this monster -- it still loses the turn (ConsumeEnergy back in Run
            // still runs), it just never gets to act during it.
            return $"{CombatMessages.Label(actor, capitalized: true)} is stunned and cannot act.";
        }

        if (actor.IsProne)
        {
            // A knocked-down monster always spends its next scheduled action standing back up
            // instead of running its normal AI -- never moves, attacks, casts, or uses another
            // ability before then.
            actor.IsProne = false;
            return $"{CombatMessages.Label(actor, capitalized: true)} gets back on its feet.";
        }

        if (level.TurnNumber < actor.FrightenedUntilTurn)
        {
            // New Priest Skill Progression (Turn Undead): retreats from (never voluntarily closes
            // on) whatever caused the fear instead of running normal AI -- see FrightenedBehavior.
            return FrightenedBehavior.TakeTurn(actor, level, rng);
        }

        return actor.AI?.TakeTurn(actor, level, rng);
    }

    /// <summary>Whether the player's own tile is currently a dark room with no illumination reaching it -- feeds the "Darkness closes around you."/"The darkness retreats from the light." transition messages (design spec section 11). Internal so Diagnostics/SelfTest.cs can verify it directly.</summary>
    internal static bool IsPlayerInDarkness(Level level, Player player)
    {
        var tile = level.Tiles[player.X, player.Y];
        return tile.IsDarkRoom && !tile.IsIlluminated;
    }

    /// <summary>
    /// True if the player themselves attacked (melee, a fired/thrown projectile, or an
    /// offensive spell -- see the LastAttackTurn set-sites) this turn or last, or if any
    /// Monster currently standing adjacent to the player did -- see ResourceRegenerationCalculator's
    /// combat-rate slowdown, the only thing this feeds into. Internal (not private) so
    /// Diagnostics/SelfTest.cs can verify it directly.
    /// </summary>
    internal static bool IsPlayerInCombat(Level level, Player player)
    {
        if (AttackedRecently(player, level.TurnNumber))
        {
            return true;
        }

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }
                if (level.GetActorAt(player.X + dx, player.Y + dy) is Monster monster && AttackedRecently(monster, level.TurnNumber))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// 0 = attacked this turn, 1 = attacked last turn -- see IsPlayerInCombat's own call-site
    /// comment for why this must run BEFORE Level.AdvanceTurn, not after. Requires the
    /// difference to be non-negative too, not just <= 1 -- Level.TurnNumber is per-level (a
    /// freshly generated floor starts back at 0), but Player.LastAttackTurn persists across
    /// floors since the Player object is never recreated on a transition. Without this guard, a
    /// player who last attacked late in the previous level's turn count (e.g. turn 95) would
    /// register as "in combat" for the entire start of the new level (0 - 95 = -95, which is
    /// <= 1) until its own turn counter climbed back past that stale value -- the exact bug
    /// report of natural regen staying stuck at the 1/10th combat rate on a fresh floor with no
    /// fighting at all. A monster's own LastAttackTurn never has this problem (it's scoped to
    /// the one Level it lives on, whose TurnNumber only ever increases), so this guard is only
    /// ever load-bearing for the player.
    /// </summary>
    private static bool AttackedRecently(Actor actor, int currentTurn) => currentTurn - actor.LastAttackTurn is >= 0 and <= 1;

    /// <summary>
    /// The one place a new status event is recorded -- keeps at most
    /// MaxStatusMessages logical messages, oldest dropped first. Never call
    /// this for "nothing happened" cases (a silent move, an unrecognized
    /// key); leaving history untouched is exactly the right behavior then,
    /// so those code paths just don't call it at all.
    /// </summary>
    private void AddStatusMessage(string message, ConsoleColor color = ConsoleColor.White) => messageLog.Add(message, color);

    /// <summary>Renders real history plus one extra prompt line that is never itself recorded -- for synchronous "choose a direction" prompts immediately superseded by the actual result, which would otherwise burn a slot in the visible message area for a UI prompt no one needs to look back at.</summary>
    private void RenderWithTransientMessage(Level level, string transientMessage)
    {
        var display = new List<MessageEntry>(messageLog.GetRecent(MessageLog.VisibleLines - 1)) { new MessageEntry(transientMessage) };
        Renderer.Render(level, player, display);
    }

    /// <returns>True if the player's input consumed a turn (so the scheduler should advance).</returns>
    private bool ProcessPlayerTurn(Level level)
    {
        if (level.TurnNumber < player.StunnedUntilTurn)
        {
            AddStatusMessage("You are stunned and cannot act.");
            Console.ReadKey(true); // still wait for a keypress so the message is actually seen before the turn burns
            return true;
        }

        if (isSleeping)
        {
            // Automatic Wait -- no message (the blacked-out map already makes it obvious nothing
            // is being decided), no ReadCommand() call at all. HandleSleepInputKey (see the Run()
            // loop) is what actually reads input while asleep.
            return true;
        }

        var command = InputHandler.ReadCommand();

        // Prone/Knockdown System: everything except Stand and a small handful of purely
        // informational/read-only commands is rejected outright while prone, without consuming a
        // turn -- see IsAllowedWhileProne's own doc comment for exactly which commands qualify.
        if (player.IsProne && command != PlayerCommand.Stand && !IsAllowedWhileProne(command))
        {
            AddStatusMessage("You are prone. You must stand first.");
            return false;
        }

        switch (command)
        {
            case PlayerCommand.Stand:
                return HandleStand();

            case PlayerCommand.Quit:
                RecordElapsedDungeonTime();
                SaveManager.Save(dungeonManager, player);
                AddStatusMessage("Game saved. Goodbye!");
                isRunning = false;
                return false;

            case PlayerCommand.Wait:
                AddStatusMessage("You wait.");
                return true;

            case PlayerCommand.Sleep:
                return HandleStartSleeping(level);

            case PlayerCommand.OpenInventory:
                // Show returns whether a turn-costing action happened (equipping/replacing/
                // removing RangedWeapon or Ammunition specifically -- see its own doc comment);
                // every other inventory action (including lighting/extinguishing a physical light
                // source) stays free, same as always.
                bool inventoryTurnConsumed = InventoryScreen.Show(player, level);
                RecomputeFov();
                return inventoryTurnConsumed;

            case PlayerCommand.ShowHelp:
                HelpScreen.Show();
                return false;

            case PlayerCommand.PickUp:
                return HandlePickUp(level);

            case PlayerCommand.DropItem:
                return HandleDropItem(level);

            case PlayerCommand.CastSpell:
                return HandleCastSpell(level);

            case PlayerCommand.Look:
                return HandleLook(level);

            case PlayerCommand.LearnSpell:
                return HandleLearnSpell();

            case PlayerCommand.UseSkill:
                return HandleUseSkill(level);

            case PlayerCommand.OpenChest:
                return HandleOpenChest(level);

            case PlayerCommand.FireProjectile:
                return HandleFireProjectile(level);

            case PlayerCommand.AscendStairs:
                return HandleUseStairs(level, descending: false);

            case PlayerCommand.DescendStairs:
                return HandleUseStairs(level, descending: true);

            case PlayerCommand.ShowMessageHistory:
                MessageHistoryScreen.Show(messageLog.History);
                return false;

            case PlayerCommand.Push:
                return HandlePush(level);

            case PlayerCommand.LookHere:
                return HandleLookHere(level);

            case PlayerCommand.ShowAdventureRecord:
                RecordElapsedDungeonTime();
                AdventureRecordScreen.Show(player);
                return false; // opening the screen never consumes a turn, same as InventoryScreen/HelpScreen

            case PlayerCommand.Search:
                return HandleSearch(level);

            case PlayerCommand.SwapWithPet:
                return HandleSwapWithPet(level);

            case PlayerCommand.None:
                return false;

            default:
                return HandleMove(level, command);
        }
    }

    /// <summary>
    /// Prone/Knockdown System: the commands that remain usable while prone -- purely informational
    /// screens (Help, Inventory viewing, Character stats, Message history) plus the two read-only
    /// Look commands, matching the proposal's own "informational screens... may still be opened."
    /// Quit and None are exempted too (saving/quitting is never a game action; None is simply no
    /// key having done anything yet). Every other command -- explicitly including Wait, Pick Up,
    /// Drop Item, Push, and Learn Spell, none of which the proposal names either way -- is treated
    /// the same as the actions it does explicitly reject (movement/attack/fire/cast/skill/
    /// container/search/sleep/stairs/equipment): rejected, so the only two things a prone player
    /// can ever spend a turn on are standing back up or doing nothing at all.
    /// </summary>
    internal static bool IsAllowedWhileProne(PlayerCommand command) => command switch
    {
        PlayerCommand.None => true,
        PlayerCommand.Quit => true,
        PlayerCommand.ShowHelp => true,
        PlayerCommand.OpenInventory => true,
        PlayerCommand.ShowMessageHistory => true,
        PlayerCommand.ShowAdventureRecord => true,
        PlayerCommand.Look => true,
        PlayerCommand.LookHere => true,
        _ => false
    };

    /// <summary>'T' -- clears IsProne and always consumes a turn; rejected for free if the player isn't actually prone. Internal so Diagnostics/SelfTest.cs can drive it directly.</summary>
    internal bool HandleStand()
    {
        if (!player.IsProne)
        {
            AddStatusMessage("You are already standing.");
            return false;
        }

        player.IsProne = false;
        AddStatusMessage("You get back on your feet.");
        return true;
    }

    /// <summary>Test-only window into sleep state -- see Diagnostics/SelfTest.cs.</summary>
    internal bool IsSleeping => isSleeping;

    /// <summary>Test-only seam so Diagnostics/SelfTest.cs can force a specific ambush turn instead of depending on GameLoop's own non-seeded internal Random actually rolling one within a reasonable number of attempts.</summary>
    internal void ForceSleepAmbushTurn(int turn) => sleepAmbushTurn = turn;

    private bool HasManaPool => player.Class.ManaStat != null;

    private bool IsAdjacentHostileMonsterPresent(Level level) =>
        level.Actors.OfType<Monster>().Any(m => m.IsAlive && Math.Max(Math.Abs(m.X - player.X), Math.Abs(m.Y - player.Y)) <= 1);

    /// <summary>
    /// How much of the HP+mana missing when sleep began has been restored so far, combined into
    /// one fraction (see SleepMessages.CalculateRecovery) so a large mana pool or health pool
    /// never dominates unfairly. Player.Health/Mana.Max is assumed unchanged since sleep began --
    /// nothing that grants XP (the only thing that raises Max) can happen while the player is
    /// asleep and not acting.
    /// </summary>
    private double CurrentSleepRecovery()
    {
        int hpRestored = player.Health.Current - sleepHpAtStart;
        int hpMissingAtStart = player.Health.Max - sleepHpAtStart;
        int manaRestored = 0;
        int manaMissingAtStart = 0;
        if (HasManaPool)
        {
            manaRestored = player.Mana.Current - sleepManaAtStart;
            manaMissingAtStart = player.Mana.Max - sleepManaAtStart;
        }
        return SleepMessages.CalculateRecovery(hpRestored, manaRestored, hpMissingAtStart, manaMissingAtStart);
    }

    private void EndSleep()
    {
        isSleeping = false;
        sleepAmbushTurn = null;
    }

    /// <summary>
    /// 'W' -- rejects (free action, no turn) if a harmful DoT is active, the player is standing on
    /// a recurring-damage tile, or HP/mana are already full; otherwise snapshots the recovery
    /// baseline, rolls the ambush chance/delay, and begins sleeping. Internal so
    /// Diagnostics/SelfTest.cs can drive it directly -- see HandleSleepInputKey/CheckSleepInterrupts
    /// for the rest of the sleep state machine.
    /// </summary>
    internal bool HandleStartSleeping(Level level)
    {
        if (player.ActiveEffects.Any(e => e.TickDamage > 0))
        {
            AddStatusMessage("Your suffering prevents you from sleeping.");
            return false;
        }

        if (FloorTypeCatalog.Get(level.Tiles[player.X, player.Y].FloorType).EnvironmentalDamageEnabled)
        {
            AddStatusMessage("The burning ground makes sleep impossible.");
            return false;
        }

        if (player.Health.Current == player.Health.Max && (!HasManaPool || player.Mana.Current == player.Mana.Max))
        {
            AddStatusMessage("You are already fully rested.");
            return false;
        }

        sleepHpAtStart = player.Health.Current;
        sleepManaAtStart = HasManaPool ? player.Mana.Current : 0;
        sleepHpSnapshot = player.Health.Current;
        sleepHadHarmfulEffect = false; // guaranteed by the rejection check above
        sleepHadAdjacentMonster = IsAdjacentHostileMonsterPresent(level);

        int luck = player.Stats.Adjusted(PrimaryAttribute.Luck);
        sleepAmbushTurn = SleepAmbushSystem.RollAmbushChance(luck, rng)
            ? level.TurnNumber + SleepAmbushSystem.RollAmbushDelay(rng)
            : null;

        isSleeping = true;
        return false;
    }

    /// <summary>
    /// Called once after every completed tick while asleep (see Run()) -- checked in this order:
    /// fully rested, a scheduled ambush arriving, damage taken (covers an existing monster's
    /// attack and environmental damage alike, since both already print their own normal message
    /// through code this feature never touches), a newly-applied harmful effect (same reasoning --
    /// its onset message already printed), then a hostile monster newly adjacent (proactive --
    /// wakes the player the tick before its own attack would land). Internal so
    /// Diagnostics/SelfTest.cs can drive it directly.
    /// </summary>
    internal void CheckSleepInterrupts(Level level)
    {
        if (player.Health.Current == player.Health.Max && (!HasManaPool || player.Mana.Current == player.Mana.Max))
        {
            AddStatusMessage("You awaken fully rested, your health and strength restored.");
            EndSleep();
            return;
        }

        if (sleepAmbushTurn.HasValue && level.TurnNumber >= sleepAmbushTurn.Value)
        {
            var result = SleepAmbushSystem.SpawnAmbush(level, player, dungeonManager.CurrentFloorIndex, rng);
            string message = SleepMessages.AmbushMessage(result.Monsters.Count, result.Boss?.DisplayName);
            double recovery = CurrentSleepRecovery();
            if (recovery > 0)
            {
                message += " " + SleepMessages.TrailingSentence(recovery);
            }
            AddStatusMessage(message);
            EndSleep();
            return;
        }

        if (player.Health.Current < sleepHpSnapshot)
        {
            AddStatusMessage(SleepMessages.TrailingSentence(CurrentSleepRecovery()));
            EndSleep();
            return;
        }

        bool hasHarmfulEffect = player.ActiveEffects.Any(e => e.TickDamage > 0);
        if (hasHarmfulEffect && !sleepHadHarmfulEffect)
        {
            AddStatusMessage(SleepMessages.TrailingSentence(CurrentSleepRecovery()));
            EndSleep();
            return;
        }

        bool hasAdjacentMonster = IsAdjacentHostileMonsterPresent(level);
        if (hasAdjacentMonster && !sleepHadAdjacentMonster)
        {
            AddStatusMessage("A nearby threat startles you awake. " + SleepMessages.TrailingSentence(CurrentSleepRecovery()));
            EndSleep();
            return;
        }

        sleepHpSnapshot = player.Health.Current;
        sleepHadHarmfulEffect = hasHarmfulEffect;
        sleepHadAdjacentMonster = hasAdjacentMonster;
    }

    internal enum SleepInputOutcome
    {
        StayAsleep,
        OpenHelp,
        Wake
    }

    /// <summary>
    /// The pure decision for one non-blockingly-peeked key while asleep, split out from
    /// HandleSleepInputKey so Diagnostics/SelfTest.cs can verify it without ever calling the
    /// blocking HelpScreen.Show(). Esc always wakes (no PlayerCommand mapping exists for it, same
    /// as everywhere else in this codebase); ShowHelp opens Help without waking or consuming a
    /// turn; None (unrecognized) is ignored; anything else recognized wakes the player.
    /// </summary>
    internal static SleepInputOutcome ResolveSleepInputOutcome(ConsoleKeyInfo key)
    {
        if (key.Key == ConsoleKey.Escape)
        {
            return SleepInputOutcome.Wake;
        }

        return InputHandler.ResolveCommand(key) switch
        {
            PlayerCommand.ShowHelp => SleepInputOutcome.OpenHelp,
            PlayerCommand.None => SleepInputOutcome.StayAsleep,
            _ => SleepInputOutcome.Wake
        };
    }

    /// <summary>
    /// Acts on ResolveSleepInputOutcome's decision -- waking without performing the key's actual
    /// action (so the player's very next real keypress is a fresh decision), opening Help without
    /// waking or consuming a turn, or doing nothing for an unrecognized key. Internal so
    /// Diagnostics/SelfTest.cs can drive it directly (see Run() for where it's actually called).
    /// </summary>
    internal void HandleSleepInputKey(ConsoleKeyInfo key)
    {
        switch (ResolveSleepInputOutcome(key))
        {
            case SleepInputOutcome.OpenHelp:
                HelpScreen.Show();
                break;
            case SleepInputOutcome.Wake:
                AddStatusMessage(SleepMessages.ManualWakeSentence(CurrentSleepRecovery()));
                EndSleep();
                break;
        }
    }

    /// <summary>`offerEquip` exists purely as a test seam (see Diagnostics/SelfTest.cs) so a test exercising unrelated pickup behavior with an otherwise-eligible wearable item never blocks on the interactive Y/N prompt's Console.ReadKey -- real gameplay always calls this with the default.</summary>
    internal bool HandlePickUp(Level level, bool offerEquip = true)
    {
        var items = level.GetItemsAt(player.X, player.Y);
        if (items.Count == 0)
        {
            AddStatusMessage("There's nothing here to pick up.");
            return false;
        }

        if (items.Count == 1)
        {
            return PickUpItem(level, items[0], offerEquip);
        }

        var labels = items.Select(i => i.DisplayName).ToList();
        int? index = MenuPrompt.Choose("Pick up which item?", labels, allowCancel: true, fixedKey: 'g', fixedKeyLabel: "Get All");
        if (index == null)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }

        return index.Value == MenuPrompt.FixedKeyIndex ? PickUpAll(level, items) : PickUpItem(level, items[index.Value], offerEquip);
    }

    /// <summary>Picks up exactly one item -- stacking (see ItemStacking) into an existing carried stack with room first, then a capacity check, then the actual inventory move, then (see OfferEquipAfterPickup) a quick offer to equip it if it's wearable and an eligible empty slot exists. Shared by the single-item-on-tile fast path and a specific choice from "Pick up which item?" -- never by "Get All" (see PickUpAll), per the Offer to Equip design doc's own explicit exclusion.</summary>
    private bool PickUpItem(Level level, Item item, bool offerEquip = true)
    {
        // Captured before RemoveItem deletes the GroundItem entry -- see CreditRecoveryIfMisplaced.
        var groundItem = level.GetGroundItemsAt(player.X, player.Y).FirstOrDefault(g => ReferenceEquals(g.Item, item));

        // Potions/scrolls/spellbooks/keys/lockpicks/throwables all combine into one running
        // Charges count instead of sitting as their own separate inventory entries -- see
        // ItemStacking. A full stack (potions cap at 5) is skipped in favor of starting a new
        // one, same as finding room in any other container. Most stackables aren't equipment,
        // but an AmmoThrown one (a Shuriken, a bundle of Arrows) is both -- so the equip offer
        // below still applies here too, just against the merged stack instead of the freshly
        // picked-up instance (which was never separately added to inventory). OfferEquipAfterPickup
        // itself is a no-op for every other stackable, since FindEligibleEmptyEquipSlot returns
        // null for anything with no EquipmentType.
        if (ItemStacking.IsStackable(item))
        {
            var existingStack = ItemStacking.FindStackWithRoom(player.Inventory.Items, item);
            if (existingStack != null)
            {
                int added = item.Charges ?? 1;
                existingStack.Charges = (existingStack.Charges ?? 1) + added;
                level.RemoveItem(item);
                AddStatusMessage($"You add {item.DisplayName} to your stack ({existingStack.Charges} total).");
                CreditRecoveryIfMisplaced(groundItem, item);
                if (offerEquip)
                {
                    OfferEquipAfterPickup(level, existingStack);
                }
                return true;
            }
        }

        if (!EncumbranceCalculator.CanCarry(player, item, out string capacityReason))
        {
            AddStatusMessage(capacityReason);
            return false;
        }

        player.Inventory.AddItem(item);
        level.RemoveItem(item);
        AddStatusMessage($"You pick up a {item.DisplayName}.");
        CreditRecoveryIfMisplaced(groundItem, item);
        if (offerEquip)
        {
            OfferEquipAfterPickup(level, item);
        }
        return true;
    }

    /// <summary>
    /// "Offer to Equip Wearable Items on Pickup" design doc: after a successful, non-stacked
    /// pickup, if the item is wearable/wieldable and has an eligible EMPTY slot (see
    /// EquipmentCompatibility.FindEligibleEmptyEquipSlot -- an occupied single-type slot like Body
    /// never counts, so this never opens the "replace which slot?" interface), ask a quick Y/N.
    /// Reading the extra key here doesn't consume its own turn -- the pickup that triggered it
    /// already will, same as HandleFireProjectile's "choose a direction" read.
    /// </summary>
    private void OfferEquipAfterPickup(Level level, Item item)
    {
        if (item.EquipmentType == EquipmentType.None)
        {
            return;
        }

        if (!item.IsIdentified)
        {
            // Item Comparison proposal's recommended rule: suppress EVERY automatic equip offer
            // -- empty-slot AND replacement -- for an unidentified item, since an unknown curse
            // could lock it on permanently. Deliberately changes existing behavior (the empty-
            // slot offer below has never had an identification check). The optional hint only
            // names a real counterpart when one actually exists -- never invents one.
            var counterpart = EquippedComparisonTargetFinder.FindComparableEquipped(player, item)
                .FirstOrDefault(m => !ReferenceEquals(m.Item, item));
            if (counterpart.Item != null)
            {
                AddStatusMessage($"Identify it to compare its full quality with your equipped {counterpart.Item.DisplayName}.");
            }
            return;
        }

        var slot = EquipmentCompatibility.FindEligibleEmptyEquipSlot(player, item);
        if (slot != null)
        {
            // EXISTING behavior, unchanged.
            RenderWithTransientMessage(level, BuildEquipOfferPrompt(item, slot.Value));
            bool accepted = Console.ReadKey(intercept: true).Key == ConsoleKey.Y;

            string message = ResolveEquipOffer(item, slot.Value, accepted);
            if (message != null)
            {
                AddStatusMessage(message);
            }
            return;
        }

        var upgradeTarget = DecidePickupUpgradeOffer(item);
        if (upgradeTarget == null)
        {
            return;
        }

        var upgradeResult = ItemComparer.Compare(item, ItemQualityCalculator.Evaluate(item),
            upgradeTarget.Value.Item, ItemQualityCalculator.Evaluate(upgradeTarget.Value.Item));
        RenderWithTransientMessage(level, BuildUpgradeOfferPrompt(item, upgradeTarget.Value, upgradeResult.Delta));
        bool acceptedUpgrade = Console.ReadKey(intercept: true).Key == ConsoleKey.Y;

        string upgradeMessage = ResolveEquipOffer(item, upgradeTarget.Value.Slot, acceptedUpgrade);
        if (upgradeMessage != null)
        {
            AddStatusMessage(upgradeMessage);
        }
    }

    /// <summary>
    /// Pure decision logic (no Console) for pickup upgrade detection -- see Diagnostics/SelfTest.cs.
    /// Finds the weakest legally-replaceable, identified equipped item sharing the picked-up
    /// item's semantic comparison role (never an assumed preferred slot -- see
    /// EquippedComparisonTargetFinder), and offers it only when the new item is a definitive,
    /// legal upgrade over that specific target.
    /// </summary>
    internal (EquipmentSlot Slot, Item Item)? DecidePickupUpgradeOffer(Item item)
    {
        if (item.EquipmentType == EquipmentType.None || !item.IsIdentified)
        {
            return null;
        }

        var target = EquippedComparisonTargetFinder.FindWeakestLegalReplacementTarget(player, item);
        if (target == null)
        {
            return null;
        }

        var result = ItemComparer.Compare(item, ItemQualityCalculator.Evaluate(item),
            target.Value.Item, ItemQualityCalculator.Evaluate(target.Value.Item));
        if (result.DefinitiveWinner != item)
        {
            return null;
        }

        // Final re-validation immediately before offering -- comparison alone is never
        // permission to equip.
        if (!ItemComparisonCompatibility.CanReplaceInContext(player, item, target.Value.Item, target.Value.Slot))
        {
            return null;
        }

        return target;
    }

    /// <summary>"The Flaming Mace is better than your equipped Iron Sword (+3 quality).\nEquip it in the Off-hand slot? (Y/N)" -- internal (not private) so Diagnostics/SelfTest.cs can verify the exact wording without driving Console.ReadKey.</summary>
    internal static string BuildUpgradeOfferPrompt(Item item, (EquipmentSlot Slot, Item Item) target, decimal delta) =>
        $"The {item.DisplayName} is better than your equipped {target.Item.DisplayName} (+{delta:0.#} quality).\n" +
        $"Equip it in the {EquipmentCompatibility.SlotLabel(target.Slot)} slot? (Y/N)";

    /// <summary>"Equip the Iron Breastplate in the Body slot? (Y/N)" (identified) / "Equip Unidentified Armor in the Body slot? (Y/N)" (not) -- DisplayName only, so an unidentified item's true Name/properties are never exposed by the prompt itself. Internal (not private) so Diagnostics/SelfTest.cs can verify the exact wording without driving Console.ReadKey.</summary>
    internal static string BuildEquipOfferPrompt(Item item, EquipmentSlot slot)
    {
        string article = item.IsIdentified ? "the " : "";
        return $"Equip {article}{item.DisplayName} in the {EquipmentCompatibility.SlotLabel(slot)} slot? (Y/N)";
    }

    /// <summary>
    /// The equip-offer's actual decision/mutation, split out from OfferEquipAfterPickup's
    /// interactive Y/N read so Diagnostics/SelfTest.cs can exercise it headlessly -- same
    /// reasoning TraderScreen.TrySell/InventoryScreen.ApplyItem are already split this way.
    /// Declining (or the caller not calling this at all) leaves inventory/equipment untouched and
    /// returns null, no message, no error -- per the design doc's own "must not display an error"
    /// rule. Accepting mirrors InventoryScreen.ApplyItem's own manual-equip sequence exactly
    /// (EquipInSlot handles stat modifiers/derived-stat recalculation internally), so the two
    /// paths behave identically once a slot is chosen -- only how the slot got chosen differs.
    /// Never touches Item.IsIdentified either way.
    /// </summary>
    internal string ResolveEquipOffer(Item item, EquipmentSlot slot, bool accepted)
    {
        if (!accepted)
        {
            return null;
        }

        var previous = player.EquipInSlot(slot, item);
        player.Inventory.RemoveItem(item);
        if (previous != null)
        {
            player.Inventory.AddItem(previous);
        }
        return $"You equip the {item.DisplayName} ({EquipmentCompatibility.SlotLabel(slot)}).";
    }

    /// <summary>
    /// Awards MisplacedItemsRecovered exactly once per GroundItem instance that was ever
    /// concealed -- groundItem is null for an ordinary item that was never concealed, in which
    /// case this is a no-op. See GroundItem.CountsAsMisplaced/HasBeenRecovered's own doc comments
    /// for why this state lives there rather than on Item (a later drop of the same physical item
    /// creates a brand new GroundItem with its own independent recovery eligibility).
    /// </summary>
    private void CreditRecoveryIfMisplaced(GroundItem groundItem, Item item)
    {
        if (groundItem == null || !groundItem.CountsAsMisplaced || groundItem.HasBeenRecovered)
        {
            return;
        }

        groundItem.HasBeenRecovered = true;
        player.AdventureRecord.MisplacedItemsRecovered++;
        AddStatusMessage(ItemLossMessages.ForRecovered(item));
    }

    /// <summary>
    /// "Get All" -- attempts every item on the tile in turn (the same lockpick-stacking/capacity
    /// rules PickUpItem applies to one), reporting a single combined summary instead of one
    /// status message per item, since AddStatusMessage's own history only ever keeps the last
    /// two anyway. Consumes a turn if at least one item was actually picked up, even when
    /// others had to be left behind for lack of carrying capacity.
    /// </summary>
    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can exercise "Get All" directly -- MenuPrompt.Choose's own key-driven selection can't be scripted from a headless test, same reasoning HandlePickUp/AwardDeathRewards were already bumped to internal for.</summary>
    internal bool PickUpAll(Level level, IReadOnlyList<Item> items)
    {
        var pickedUp = new List<string>();
        string capacityFailureReason = null;

        foreach (var item in items)
        {
            // Captured before RemoveItem deletes the GroundItem entry -- see CreditRecoveryIfMisplaced.
            var groundItem = level.GetGroundItemsAt(player.X, player.Y).FirstOrDefault(g => ReferenceEquals(g.Item, item));

            if (ItemStacking.IsStackable(item))
            {
                // Stacks against what's already been picked up earlier in this SAME "Get All"
                // batch too, not just what was already in inventory before it started --
                // player.Inventory.Items already reflects every earlier iteration's AddItem.
                var existingStack = ItemStacking.FindStackWithRoom(player.Inventory.Items, item);
                if (existingStack != null)
                {
                    int added = item.Charges ?? 1;
                    existingStack.Charges = (existingStack.Charges ?? 1) + added;
                    level.RemoveItem(item);
                    pickedUp.Add(item.DisplayName);
                    CreditRecoveryIfMisplaced(groundItem, item);
                    continue;
                }
            }

            if (!EncumbranceCalculator.CanCarry(player, item, out string capacityReason))
            {
                capacityFailureReason ??= capacityReason;
                continue;
            }

            player.Inventory.AddItem(item);
            level.RemoveItem(item);
            pickedUp.Add(item.DisplayName);
            CreditRecoveryIfMisplaced(groundItem, item);
        }

        if (pickedUp.Count == 0)
        {
            AddStatusMessage(capacityFailureReason ?? "You couldn't pick up anything here.");
            return false;
        }

        string message = $"You pick up {string.Join(", ", pickedUp)}.";
        if (capacityFailureReason != null)
        {
            message += $" ({capacityFailureReason})";
        }
        AddStatusMessage(message);
        return true;
    }

    /// <summary>
    /// Only ever offers items already in player.Inventory.Items -- an
    /// equipped item lives in the separate player.Equipment collection and
    /// is never in that list, so it's structurally impossible to reach an
    /// equipped item here without unequipping it first. No special-case
    /// check needed to enforce "unequip before dropping."
    /// </summary>
    private bool HandleDropItem(Level level)
    {
        if (player.Inventory.Items.Count == 0)
        {
            AddStatusMessage("You have nothing to drop.");
            return false;
        }

        var item = player.Inventory.Items.Count == 1
            ? player.Inventory.Items[0]
            : ChooseItem(player.Inventory.Items, "Drop which item?");
        if (item == null)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }

        Item dropped;
        int quantity = 1;
        if (ItemStacking.IsStackable(item) && (item.Charges ?? 1) > 1)
        {
            int? chosen = PromptDropQuantity(item.Charges!.Value);
            if (chosen == null)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }
            quantity = chosen.Value;

            // Splits off exactly the chosen quantity into a standalone bundle -- ConsumeMany
            // shrinks (or removes) the carried stack, mirroring how ThrowItem's own ConsumeOne
            // never touches the rest of a throwable stack.
            dropped = item.Clone();
            dropped.Charges = quantity;
            ItemStacking.ConsumeMany(player.Inventory, item, quantity);
        }
        else
        {
            dropped = item;
            player.Inventory.RemoveItem(item);
        }

        var landing = ItemLandingResolver.ResolveDropped(player, dropped, level, player.X, player.Y, quantity, rng);
        AddStatusMessage(landing.Message
            ?? (quantity > 1 ? $"You drop {quantity} {dropped.DisplayName}s." : $"You drop the {dropped.DisplayName}."));
        return true;
    }

    /// <returns>Null if the player cancelled (Esc) instead of choosing a quantity. Only ever prompted for a stackable item currently carrying more than one unit -- see HandleDropItem.</returns>
    private static int? PromptDropQuantity(int maxCount)
    {
        var labels = Enumerable.Range(1, maxCount).Select(n => n.ToString()).ToList();
        int? index = MenuPrompt.Choose("Drop how many?", labels, allowCancel: true);
        return index.HasValue ? index.Value + 1 : null;
    }

    /// <returns>Null if the player cancelled (Esc) instead of picking one.</returns>
    private static Item ChooseItem(IReadOnlyList<Item> items, string prompt)
    {
        var labels = items.Select(i => i.DisplayName).ToList();
        int? index = MenuPrompt.Choose(prompt, labels, allowCancel: true);
        return index.HasValue ? items[index.Value] : null;
    }

    /// <summary>Inventory + equipped items not yet identified -- shared by the Identify spell and Items.ScrollOfIdentify (InventoryScreen.ApplyItem has its own copy of this same filter).</summary>
    private List<Item> UnidentifiedItemCandidates() =>
        player.Inventory.Items
            .Concat(player.Equipment.AllEquipped.Select(kvp => kvp.Value))
            .Where(i => !i.IsIdentified)
            .ToList();

    private readonly record struct Castable(Spell Spell, Item SourceItem, string Label);

    /// <returns>True if a spell was actually cast (so the scheduler advances); false on cancel or a failed cast (no mana/cooldown/free action, per "failed casts don't consume anything").</returns>
    private bool HandleCastSpell(Level level)
    {
        var castables = BuildCastableList();
        if (castables.Count == 0)
        {
            AddStatusMessage("You don't know any spells or carry anything that casts one.");
            return false;
        }

        var labels = castables.Select(c => c.Label).ToList();
        int? index = MenuPrompt.Choose("Cast which spell?", labels, allowCancel: true);
        if (index == null)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }
        var chosen = castables[index.Value];

        // Class/level gating only applies to known spells -- a wand's charge bypasses
        // it entirely, same as it already bypasses mana/cooldown (see SpellCaster.Cast).
        if (chosen.SourceItem == null && !SpellRequirementValidator.CanCast(player, chosen.Spell, out string ineligibleReason))
        {
            AddStatusMessage(ineligibleReason);
            return false;
        }

        SpellCastingContext context;
        int aimDx = 0, aimDy = 0;
        if (chosen.Spell.Targeting.TargetType == SpellTargetType.Self)
        {
            // No direction to aim -- a Self spell always affects the caster, so skip
            // straight to casting instead of asking for an aim that would be ignored.
            context = BuildContext(level, chosen.Spell, 0, 0);
        }
        else if (chosen.Spell.Targeting.TargetType == SpellTargetType.Item)
        {
            // No direction to aim either -- prompt for which unidentified item to target instead.
            var candidates = UnidentifiedItemCandidates();
            if (candidates.Count == 0)
            {
                AddStatusMessage("You have nothing unidentified to identify.");
                return false;
            }

            var itemLabels = candidates.Select(i => i.DisplayName).ToList();
            int? itemIndex = MenuPrompt.Choose("Identify which item?", itemLabels, allowCancel: true);
            if (itemIndex == null)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }

            context = BuildContext(level, chosen.Spell, 0, 0);
            context.TargetItem = candidates[itemIndex.Value];
        }
        else
        {
            RenderWithTransientMessage(level, "Choose a direction to aim, or any other key to cancel...");
            var (dx, dy) = InputHandler.ToDelta(InputHandler.ReadCommand());
            if (dx == 0 && dy == 0)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }

            aimDx = dx;
            aimDy = dy;
            context = BuildContext(level, chosen.Spell, dx, dy);
        }

        Action<SpellCastingContext> onTargetsResolved = null;
        if (chosen.Spell.IsProjectile && (aimDx != 0 || aimDy != 0))
        {
            onTargetsResolved = ctx => AnimateSpellProjectile(level, chosen.Spell, ctx, aimDx, aimDy);
        }

        var result = SpellCaster.Cast(chosen.Spell, context, consumesMana: chosen.SourceItem == null, onTargetsResolved);

        if (result.Success && chosen.Spell.IsProjectile)
        {
            player.LastAttackTurn = level.TurnNumber;
        }

        if (result.Success && chosen.SourceItem != null)
        {
            ConsumeCharge(chosen.SourceItem);
        }

        AddStatusMessage(result.Message);

        PrimaryAttribute governingAttribute = chosen.Spell.GoverningAttribute ?? player.Class.ManaStat ?? PrimaryAttribute.Knowledge;
        ProficiencyTracker.Process(player, chosen.Spell.ProficiencyId, chosen.Spell.Name, governingAttribute, chosen.Spell.Level,
            result, ResolveTargetMonsterLevel(context), rng, AddStatusMessage);

        return result.Success;
    }

    /// <summary>Ability Proficiency System: the challenge-eligibility multiplier's "target" -- the directly aimed actor if there is one, else the highest-level monster actually caught by an AoE/tile cast, else null (a self/utility ability with nothing to be "too easy" relative to).</summary>
    private static int? ResolveTargetMonsterLevel(SpellCastingContext context) =>
        (context.TargetActor as Monster)?.Level ?? context.AffectedActors?.OfType<Monster>().OrderByDescending(m => m.Level).FirstOrDefault()?.Level;

    private List<Castable> BuildCastableList()
    {
        var list = new List<Castable>();

        foreach (var spell in player.KnownSpells)
        {
            list.Add(new Castable(spell, null, $"{spell.Name} ({spell.Casting.ManaCost} MP) - {spell.Description}"));
        }

        foreach (var item in player.Inventory.Items)
        {
            if (item.CastsSpell != null)
            {
                list.Add(new Castable(item.CastsSpell, item, $"{item.DisplayName} ({item.Charges} charges) - {item.CastsSpell.Description}"));
            }
        }

        return list;
    }

    private SpellCastingContext BuildContext(Level level, Spell spell, int dx, int dy)
    {
        var context = new SpellCastingContext(player, level, level.TurnNumber, rng)
        {
            RankScaling = RankScalingFor(spell.ProficiencyId)
        };
        int range = spell.Targeting.EffectiveRange(context.RankScaling);

        switch (spell.Targeting.TargetType)
        {
            case SpellTargetType.SingleTarget:
                context.TargetActor = RayTracer.FindActorAlongRay(level, player.X, player.Y, dx, dy, range);
                break;

            case SpellTargetType.Tile:
                context.TargetTile = RayTracer.FindTileAlongRay(level, player.X, player.Y, dx, dy, range);
                break;
        }

        return context;
    }

    /// <summary>Ability Proficiency System: RankScaling.Neutral for an unranked ability (null/empty id) -- see Player.RankOf.</summary>
    private RankScaling RankScalingFor(string proficiencyId) => new(player.RankOf(proficiencyId));

    /// <summary>
    /// Plays a purely visual flight for a projectile spell -- TargetResolver has already
    /// committed to the affected actors/tile by the time SpellCaster.Cast invokes this (see
    /// its onTargetsResolved parameter), so this never decides hit/miss; it only shows the
    /// trip there before DamageEffect applies the already-resolved damage right after.
    /// </summary>
    private void AnimateSpellProjectile(Level level, Spell spell, SpellCastingContext context, int dx, int dy)
    {
        var damageEffect = spell.Effects.OfType<DamageEffect>().FirstOrDefault();
        if (damageEffect == null)
        {
            return;
        }

        var (glyph, color) = ProjectileVisuals.ForDamageType(damageEffect.DamageType);
        var definition = new ProjectileDefinition(spell.Name, spell.Targeting.Range, penetration: 0, glyph, color, AttackType.Hit);

        (int X, int Y) destination = context.TargetTile ?? (context.TargetActor?.X ?? context.Caster.X, context.TargetActor?.Y ?? context.Caster.Y);
        ProjectileEngine.AnimateFlight(level, player, messageLog.GetRecent(MessageLog.VisibleLines), definition, context.Caster.X, context.Caster.Y, (dx, dy), destination.X, destination.Y);
    }

    private readonly record struct Usable(Skill Skill, string Label);

    /// <summary>Mirrors HandleCastSpell closely -- see SkillCaster/Skill for how physical skills differ from Spell (no mana, deterministic level-grant instead of scroll-learned).</summary>
    private bool HandleUseSkill(Level level)
    {
        var usables = BuildUsableSkillList();
        if (usables.Count == 0)
        {
            AddStatusMessage("You don't know any usable skills.");
            return false;
        }

        var labels = usables.Select(u => u.Label).ToList();
        int? index = MenuPrompt.Choose("Use which skill?", labels, allowCancel: true);
        if (index == null)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }
        var chosen = usables[index.Value];

        if (!SkillRequirementValidator.CanUse(player, chosen.Skill, out string ineligibleReason))
        {
            AddStatusMessage(ineligibleReason);
            return false;
        }

        SpellCastingContext context;
        if (chosen.Skill.Targeting.TargetType == SpellTargetType.Self)
        {
            context = BuildSkillContext(level, chosen.Skill, 0, 0);
        }
        else if (chosen.Skill.Targeting.TargetType == SpellTargetType.Item)
        {
            // Mirrors HandleCastSpell's Identify branch -- no direction to aim either.
            var candidates = UnidentifiedItemCandidates();
            if (candidates.Count == 0)
            {
                AddStatusMessage("You have nothing unidentified to identify.");
                return false;
            }

            var itemLabels = candidates.Select(i => i.DisplayName).ToList();
            int? itemIndex = MenuPrompt.Choose("Identify which item?", itemLabels, allowCancel: true);
            if (itemIndex == null)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }

            context = BuildSkillContext(level, chosen.Skill, 0, 0);
            context.TargetItem = candidates[itemIndex.Value];
        }
        else
        {
            RenderWithTransientMessage(level, "Choose a direction to aim, or any other key to cancel...");
            var (dx, dy) = InputHandler.ToDelta(InputHandler.ReadCommand());
            if (dx == 0 && dy == 0)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }

            context = BuildSkillContext(level, chosen.Skill, dx, dy);
        }

        var result = SkillCaster.Cast(chosen.Skill, context);
        if (result.Success && chosen.Skill.Effects.Any(e => e is WeaponDamageEffect or ExecuteInstakillEffect))
        {
            player.LastAttackTurn = level.TurnNumber;
        }
        AddStatusMessage(result.Message);

        PrimaryAttribute governingAttribute = chosen.Skill.GoverningAttribute ?? PrimaryAttribute.Strength;
        ProficiencyTracker.Process(player, chosen.Skill.ProficiencyId, chosen.Skill.Name, governingAttribute, chosen.Skill.LevelFor(player.Class),
            result, ResolveTargetMonsterLevel(context), rng, AddStatusMessage);

        return result.Success;
    }

    /// <summary>Passives never appear here -- they're always-on the moment they're granted, never "used" on demand. See Skill.IsPassive.</summary>
    private List<Usable> BuildUsableSkillList()
    {
        var list = new List<Usable>();
        foreach (var skill in player.KnownSkills)
        {
            if (!skill.IsPassive)
            {
                list.Add(new Usable(skill, $"{skill.Name} - {skill.Description}"));
            }
        }
        return list;
    }

    private SpellCastingContext BuildSkillContext(Level level, Skill skill, int dx, int dy)
    {
        var context = new SpellCastingContext(player, level, level.TurnNumber, rng)
        {
            RankScaling = RankScalingFor(skill.ProficiencyId)
        };
        int range = skill.Targeting.EffectiveRange(context.RankScaling);

        switch (skill.Targeting.TargetType)
        {
            case SpellTargetType.SingleTarget:
                context.TargetActor = RayTracer.FindActorAlongRay(level, player.X, player.Y, dx, dy, range);
                break;

            case SpellTargetType.Tile:
                context.TargetTile = RayTracer.FindTileAlongRay(level, player.X, player.Y, dx, dy, range);
                break;
        }

        return context;
    }

    /// <summary>
    /// "Ranged Weapon, Ammunition, and Readied Throwable Equipment" design doc: fires or throws
    /// whatever's readied in EquipmentSlot.Ammunition -- no more picking from the full inventory
    /// each time (see the old ThrowItem's MenuPrompt.Choose, now removed). If the readied item has
    /// an AmmunitionType AND a compatible RangedWeapon is equipped (RequiredAmmunitionType
    /// matches), it fires through the launcher; otherwise, if it's independently throwable, it's
    /// thrown directly and any equipped launcher is ignored (a Dart readied while a Bow is
    /// equipped just gets thrown -- the bow never enters into it). Consumes a turn on a successful
    /// shot/throw; every rejection (nothing readied, incompatible, cancelled aim) is free, matching
    /// how a blocked action never costs anything elsewhere in this loop.
    /// </summary>
    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can verify a rejection path never consumes a turn without needing to drive the interactive aim step -- every rejection returns before ever reading a direction.</summary>
    internal bool HandleFireProjectile(Level level)
    {
        var decision = DecideFireProjectileAction(out string rejectionMessage);
        switch (decision)
        {
            case FireProjectileDecision.FireThroughLauncher:
                return FireAmmoWeapon(level, player.Equipment.Get(EquipmentSlot.RangedWeapon), player.Equipment.Get(EquipmentSlot.Ammunition));
            case FireProjectileDecision.ThrowDirectly:
                return ThrowReadiedItem(level, player.Equipment.Get(EquipmentSlot.Ammunition));
            default:
                AddStatusMessage(rejectionMessage);
                return false;
        }
    }

    internal enum FireProjectileDecision
    {
        NothingReadied,
        FireThroughLauncher,
        ThrowDirectly,
        RequiresLauncher,
        IncompatibleWithEquippedLauncher,
        ClassCannotThrowReadiedItem,
        CannotFireOrThrowReadiedItem
    }

    /// <summary>
    /// The pure routing decision for 'F', split out from HandleFireProjectile's interactive
    /// aim-then-resolve execution so Diagnostics/SelfTest.cs can verify every rejection path
    /// without needing to drive Console.ReadKey for a direction -- same reasoning
    /// InventoryScreen.ApplyItem/GameLoop.ResolveEquipOffer are already split this way. Only
    /// FireThroughLauncher/ThrowDirectly ever reach the interactive aim step; every other outcome
    /// is a free rejection with its own message, decided entirely from current equipment state.
    /// </summary>
    internal FireProjectileDecision DecideFireProjectileAction(out string rejectionMessage)
    {
        rejectionMessage = null;
        var readied = player.Equipment.Get(EquipmentSlot.Ammunition);
        if (readied == null)
        {
            rejectionMessage = "You have no ammunition or thrown weapon readied.";
            return FireProjectileDecision.NothingReadied;
        }

        var launcher = player.Equipment.Get(EquipmentSlot.RangedWeapon);
        bool launcherCompatible = launcher != null && readied.AmmunitionType.HasValue && launcher.RequiredAmmunitionType == readied.AmmunitionType;

        if (launcherCompatible)
        {
            if (!ItemRequirementValidator.CanUse(player, launcher, out rejectionMessage))
            {
                return FireProjectileDecision.IncompatibleWithEquippedLauncher;
            }
            return FireProjectileDecision.FireThroughLauncher;
        }

        if (readied.CanBeThrown)
        {
            // ThrowableCategory.Light's own doc comment: "No training needed -- anyone can chuck
            // one" -- that's a property of the category itself, not something each class has to
            // separately opt into (Warrior/Priest both omitted it from their allow-lists, which
            // wrongly blocked a starting Rock/Dart from ever being thrown).
            bool classCanThrow = readied.ThrowableCategory == ThrowableCategory.Light
                || (readied.ThrowableCategory.HasValue && player.Class.AllowedThrowableCategories.Contains(readied.ThrowableCategory.Value));
            if (!classCanThrow)
            {
                rejectionMessage = $"Your class cannot throw {readied.DisplayName}.";
                return FireProjectileDecision.ClassCannotThrowReadiedItem;
            }
            return FireProjectileDecision.ThrowDirectly;
        }

        if (readied.AmmunitionType.HasValue)
        {
            rejectionMessage = launcher == null
                ? $"{readied.AmmunitionType.Value.PluralName()} require {readied.AmmunitionType.Value.RequiredLauncherPhrase()}."
                : "That ammunition cannot be fired from your equipped weapon.";
            return launcher == null ? FireProjectileDecision.RequiresLauncher : FireProjectileDecision.IncompatibleWithEquippedLauncher;
        }

        rejectionMessage = $"You cannot fire or throw {readied.DisplayName}.";
        return FireProjectileDecision.CannotFireOrThrowReadiedItem;
    }

    private bool FireAmmoWeapon(Level level, Item weapon, Item readiedAmmo)
    {
        RenderWithTransientMessage(level, "Choose a direction to fire, or any other key to cancel...");
        var (dx, dy) = InputHandler.ToDelta(InputHandler.ReadCommand());
        if (dx == 0 && dy == 0)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }

        // Clone the exact fired unit BEFORE consuming from the readied stack, so its own
        // identification/blessed state travels with the actual projectile payload -- see
        // ProjectileFactory.ForWeaponAndAmmo (unified projectile payload, design doc section 13).
        var fired = readiedAmmo.Clone();
        fired.Charges = 1;
        bool slotCleared = ItemStacking.ConsumeOneFromEquippedSlot(player, EquipmentSlot.Ammunition);

        player.LastAttackTurn = level.TurnNumber;
        var projectile = ProjectileFactory.ForWeaponAndAmmo(player, weapon, fired, player.X, player.Y, (dx, dy));
        var outcome = ProjectileEngine.Launch(level, player, messageLog.GetRecent(MessageLog.VisibleLines), projectile, rng);

        // Fired ammo now enters the same landing/breakage/loss/concealment pipeline a thrown item
        // already goes through (design doc section 14) -- previously it fired-and-forgot with no
        // recovery possible at all.
        var landing = ItemLandingResolver.ResolveThrown(player, fired, outcome.Reason, level, projectile.X, projectile.Y, rng);
        if (landing.Message != null)
        {
            AddStatusMessage(landing.Message);
        }

        foreach (var message in outcome.Projectile.Messages)
        {
            AddStatusMessage(message);
        }

        if (slotCleared)
        {
            AddStatusMessage($"You fire your last {fired.DisplayName}.");
        }
        return true;
    }

    private bool ThrowReadiedItem(Level level, Item readiedItem)
    {
        RenderWithTransientMessage(level, "Choose a direction to throw, or any other key to cancel...");
        var (dx, dy) = InputHandler.ToDelta(InputHandler.ReadCommand());
        if (dx == 0 && dy == 0)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }

        // Splits exactly one unit off the readied stack to actually throw -- ConsumeOneFromEquippedSlot
        // shrinks (or clears) the equipped stack, while `thrown` is a standalone single-charge clone
        // representing just the unit in flight/landing, so throwing 1 of 5 darts never destroys the
        // other 4.
        var thrown = readiedItem.Clone();
        thrown.Charges = 1;
        bool slotCleared = ItemStacking.ConsumeOneFromEquippedSlot(player, EquipmentSlot.Ammunition);

        player.LastAttackTurn = level.TurnNumber;
        var projectile = ProjectileFactory.ForThrownItem(player, thrown, player.X, player.Y, (dx, dy));
        var outcome = ProjectileEngine.Launch(level, player, messageLog.GetRecent(MessageLog.VisibleLines), projectile, rng);

        bool returnedToThrower = thrown.ThrownWeaponBehavior == ThrownWeaponBehavior.ReturnsToThrower;
        switch (thrown.ThrownWeaponBehavior)
        {
            case ThrownWeaponBehavior.ReturnsToThrower:
                ReadyOrMergeIntoAmmoSlot(thrown);
                break;
            case ThrownWeaponBehavior.DropsAtImpactPoint:
                var landing = ItemLandingResolver.ResolveThrown(player, thrown, outcome.Reason, level, projectile.X, projectile.Y, rng);
                if (landing.Message != null)
                {
                    AddStatusMessage(landing.Message);
                }
                break;
            // Destroyed: already consumed from the slot above, never placed anywhere.
        }

        foreach (var message in outcome.Projectile.Messages)
        {
            AddStatusMessage(message);
        }

        if (slotCleared && !returnedToThrower)
        {
            AddStatusMessage($"You throw your last {thrown.DisplayName}.");
        }
        return true;
    }

    /// <summary>Decrements a charge-limited inventory item (a wand cast, a lockpick) and removes it once exhausted -- distinct from ItemStacking.ConsumeOneFromEquippedSlot, which does the same thing for a stack sitting in an EQUIPMENT slot instead of loose inventory.</summary>
    private void ConsumeCharge(Item item)
    {
        if (item.Charges == null)
        {
            return;
        }

        item.Charges--;
        if (item.Charges <= 0)
        {
            player.Inventory.RemoveItem(item);
        }
    }

    /// <summary>A ReturnsToThrower item reappears directly in the Ammunition slot it was thrown from -- merging onto a matching stack still readied there (the throw only ever peeled off one unit), re-readying it outright if the slot is now empty, or falling back to inventory in the rare case something else already occupies it.</summary>
    private void ReadyOrMergeIntoAmmoSlot(Item item)
    {
        var current = player.Equipment.Get(EquipmentSlot.Ammunition);
        if (current != null && ItemStacking.CanStackTogether(current, item))
        {
            current.Charges = (current.Charges ?? 1) + (item.Charges ?? 1);
            return;
        }
        if (current == null)
        {
            player.EquipInSlot(EquipmentSlot.Ammunition, item);
            return;
        }

        var existingStack = ItemStacking.FindStackWithRoom(player.Inventory.Items, item);
        if (existingStack != null)
        {
            existingStack.Charges = (existingStack.Charges ?? 1) + (item.Charges ?? 1);
        }
        else
        {
            player.Inventory.AddItem(item);
        }
    }

    /// <summary>Reads a spell scroll or spellbook from inventory, permanently learning its spell. Consumes a turn on success, same as PickUp/CastSpell; every other rejection path is free, matching how a blocked action never costs anything elsewhere in this loop.</summary>
    private bool HandleLearnSpell()
    {
        if (!player.Class.IsSpellcaster)
        {
            AddStatusMessage("Only a spellcaster can learn spells.");
            return false;
        }

        var learnables = player.Inventory.Items.Where(i => i.TeachesSpell != null).ToList();
        if (learnables.Count == 0)
        {
            AddStatusMessage("You have no spellbooks or scrolls to learn from.");
            return false;
        }

        var labels = learnables.Select(s => $"{s.Name} (requires level {s.MinimumLevel})").ToList();
        int? index = MenuPrompt.Choose("Read which item?", labels, allowCancel: true);
        if (index == null)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }
        var learnable = learnables[index.Value];
        var spell = learnable.TeachesSpell;

        if (player.KnownSpells.Contains(spell))
        {
            AddStatusMessage($"You already know {spell.Name}.");
            return false;
        }

        if (!ItemRequirementValidator.CanUse(player, learnable, out string levelReason))
        {
            AddStatusMessage(levelReason);
            return false;
        }

        if (!spell.CanBeCastBy(player.Class))
        {
            AddStatusMessage($"Your class cannot learn {spell.Name}.");
            return false;
        }

        player.KnownSpells.Add(spell);
        ItemStacking.ConsumeOne(player.Inventory, learnable);
        AddStatusMessage($"You learn {spell.Name}!");
        return true;
    }

    /// <summary>Free action (doesn't consume a turn) -- prompts for a direction, then reports the closest visible actor and/or item along that line. Description-level selection (adjacent vs. distant) is centralized in LookService.</summary>
    private bool HandleLook(Level level)
    {
        RenderWithTransientMessage(level, "Look in which direction, or any other key to cancel...");
        var (dx, dy) = InputHandler.ToDelta(InputHandler.ReadCommand());
        if (dx == 0 && dy == 0)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }

        var actor = LookService.FindClosestVisibleActor(level, player.X, player.Y, dx, dy);
        var (tileX, tileY) = LookService.FindLookTargetTile(level, player.X, player.Y, dx, dy);
        var item = level.GetItemAt(tileX, tileY);
        var roomObject = level.GetRoomObjectAt(tileX, tileY);

        if (actor != null && item != null)
        {
            int? choice = MenuPrompt.Choose(
                $"There's {CombatMessages.WithArticle(actor)} and a {item.DisplayName} here. Look at:",
                new[] { actor.DisplayName, item.DisplayName }, allowCancel: true);
            AddStatusMessage(choice == null ? "Cancelled." : choice == 0 ? LookService.DescribeCharacter(player, actor, level.TurnNumber) : item.LongDescription);
        }
        else if (actor != null)
        {
            AddStatusMessage(LookService.DescribeCharacter(player, actor, level.TurnNumber));
        }
        else if (item != null)
        {
            // Ground Look never shows the full stat breakdown (unchanged, deliberate) -- only
            // this one lightweight comparison line is added on top of the existing description,
            // and only when the item is identified or a hedged known-only comparison applies.
            string comparison = ItemComparisonFormatter.FormatLightweightLine(player, item);
            AddStatusMessage(comparison == null ? item.LongDescription : $"{item.LongDescription} | {comparison}");
        }
        else if (roomObject != null)
        {
            // Room objects and items never share a tile (every catalog object currently blocks
            // movement, so nothing can be lying on top of one) -- no three-way disambiguation
            // menu needed here the way actor+item above requires one.
            AddStatusMessage(roomObject.LongDescription);
        }
        else
        {
            AddStatusMessage("You see nothing of interest in that direction.");
        }

        return false;
    }

    /// <summary>
    /// Push (Room Objects spec): prompts for a direction, then attempts to move whatever
    /// RoomObject sits adjacent in that direction exactly one further tile the same way -- the
    /// player never moves, only the object does. All eight directions are supported, same as
    /// Look/aiming, since nothing about pushing is inherently cardinal-only. Free-action
    /// semantics mirror Look: cancelling, "nothing there," "can't be moved," and "can't move that
    /// way" all consume no turn -- only an object that actually relocates does.
    /// </summary>
    private bool HandlePush(Level level)
    {
        RenderWithTransientMessage(level, "Push in which direction, or any other key to cancel...");
        var (dx, dy) = InputHandler.ToDelta(InputHandler.ReadCommand());
        if (dx == 0 && dy == 0)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }

        int objectX = player.X + dx;
        int objectY = player.Y + dy;
        var roomObject = level.GetRoomObjectAt(objectX, objectY);
        if (roomObject == null)
        {
            AddStatusMessage("There is nothing there to push.");
            return false;
        }

        if (!roomObject.IsMovable)
        {
            AddStatusMessage($"The {roomObject.DisplayName} cannot be moved.");
            return false;
        }

        int destX = objectX + dx;
        int destY = objectY + dy;
        if (level.IsBlockedForObjectPlacement(destX, destY))
        {
            AddStatusMessage($"The {roomObject.DisplayName} cannot move in that direction.");
            return false;
        }

        var previousPosition = (roomObject.X, roomObject.Y);
        roomObject.X = destX;
        roomObject.Y = destY;
        AddStatusMessage($"You push the {roomObject.DisplayName} one tile.");

        string triggerMessage = RoomObjectTriggerProcessor.ProcessAfterMove(roomObject, previousPosition, level, rng);
        if (triggerMessage != null)
        {
            AddStatusMessage(triggerMessage);
        }

        // A trigger effect (e.g. a revealed hidden door) can open a brand-new sightline the
        // moment it happens -- refresh FOV immediately rather than waiting for the player's
        // next ordinary move.
        RecomputeFov();
        return true;
    }

    /// <summary>
    /// NumPad5 "Look Here": summarizes the player's own current tile -- unlike the directional
    /// Look command, which aims elsewhere, this always describes exactly where the player is
    /// standing. A free action, like Look; never consumes a turn. Items are withheld while
    /// standing on an unilluminated dark tile, the same rule the always-on floor-item HUD line
    /// already follows (design spec section 12/13) -- you can still feel around for them
    /// (HandlePickUp still works), this summary just can't tell you what's there before you do.
    /// </summary>
    internal bool HandleLookHere(Level level)
    {
        var tile = level.Tiles[player.X, player.Y];
        bool hiddenByDarkness = tile.IsDarkRoom && !tile.IsIlluminated;

        var effects = new List<string>();
        if (tile.FloorType != FloorType.Normal)
        {
            effects.Add($"{tile.FloorType} floor");
        }
        if (hiddenByDarkness)
        {
            effects.Add("Dark");
        }
        if (level.GetTrapAt(player.X, player.Y) != null)
        {
            effects.Add("Trap");
        }

        // Looking here is itself a discovery trigger for anything concealed underfoot -- touch,
        // not sight, so this rolls even while hiddenByDarkness suppresses the ordinary item
        // listing below (see the design doc's "You feel a small object beneath your boot" example).
        foreach (var concealed in level.GetGroundItemsAt(player.X, player.Y).Where(g => g.IsConcealed))
        {
            if (ItemDiscoveryRules.RollPassiveDiscovery(player, concealed, playerStandingOnTile: true, rng))
            {
                concealed.IsConcealed = false;
            }
        }

        var items = hiddenByDarkness ? new List<Item>() : level.GetItemsAt(player.X, player.Y);
        // Corpse System: portable (emptied) corpses are already ordinary GroundItems, so they're
        // already covered by `items` above -- this is only the loot-bearing corpses still lying
        // here as their own world container, per section 11's "Here: 2 corpses, ..." example.
        var corpses = hiddenByDarkness ? new List<Corpse>() : level.GetCorpsesAt(player.X, player.Y);

        var lines = new List<string>();
        if (effects.Count > 0)
        {
            lines.Add($"Room effects ({effects.Count}):");
            lines.AddRange(effects);
        }
        if (corpses.Count > 0)
        {
            if (lines.Count > 0)
            {
                lines.Add("");
            }
            int lootBearing = corpses.Count(c => c.Container.Contents.Items.Count > 0);
            lines.Add(lootBearing > 0
                ? $"corpses here ({corpses.Count}, {lootBearing} with items):"
                : $"corpses here ({corpses.Count}):");
            lines.AddRange(corpses.Select(c => CorpseItemFactory.FormatName(c.Metadata)));
        }
        if (items.Count > 0)
        {
            if (lines.Count > 0)
            {
                lines.Add("");
            }
            lines.Add($"items in room ({items.Count}):");
            lines.AddRange(items.Select(i => i.DisplayName));
        }

        AddStatusMessage(lines.Count > 0 ? string.Join("\n", lines) : "Nothing unusual here.");
        return false;
    }

    private static readonly (int Dx, int Dy)[] SearchOffsets =
    {
        (0, 0), (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)
    };

    /// <summary>
    /// 'X' Search -- Misplaced Items design doc. Rolls ItemDiscoveryRules.RollActiveSearch for
    /// every concealed GroundItem on the player's own tile and its 8 neighbors, revealing every
    /// success. Always consumes a turn, even when nothing is found or nothing was there to find --
    /// otherwise searching would be a free, consequence-less way to fish for a good roll.
    /// </summary>
    internal bool HandleSearch(Level level)
    {
        var found = new List<string>();
        foreach (var (dx, dy) in SearchOffsets)
        {
            int x = player.X + dx, y = player.Y + dy;
            if (!level.IsInBounds(x, y))
            {
                continue;
            }

            foreach (var groundItem in level.GetGroundItemsAt(x, y).Where(g => g.IsConcealed))
            {
                if (ItemDiscoveryRules.RollActiveSearch(player, groundItem, rng))
                {
                    groundItem.IsConcealed = false;
                    found.Add(groundItem.Item.DisplayName);
                }
            }
        }

        AddStatusMessage(found.Count switch
        {
            0 => "You search the surrounding area but find nothing.",
            1 => $"You search carefully and discover {CombatMessages.WithArticle(found[0])}.",
            _ => $"You search the area and uncover {string.Join(", ", found.Take(found.Count - 1))} and {found[^1]}."
        });
        return true;
    }

    private const int TrapRevealRadius = 3;

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can drive a bump-attack directly -- see BumpAttackingAnInvisibleOccupantNeverRevealsItsName.</summary>
    internal bool HandleMove(Level level, PlayerCommand command)
    {
        var (dx, dy) = InputHandler.ToDelta(command);
        int targetX = player.X + dx;
        int targetY = player.Y + dy;

        var occupant = level.GetActorAt(targetX, targetY);
        if (occupant is Trader trader)
        {
            TraderScreen.Show(player, trader, level);
            return false; // opening the screen never consumes a turn, same as InventoryScreen/HelpScreen
        }
        // Pet and Companion System: "the player must not automatically attack a friendly pet by
        // bumping into it" -- the pet steps aside instead, so the player can walk through onto
        // its now-vacated tile, same as the ordinary walkable-move path below.
        if (occupant is Pet occupantPet && Allegiance.AreFriendly(player, occupantPet))
        {
            if (!DisplacePetOutOfTheWay(occupantPet, level))
            {
                AddStatusMessage($"There's no room for {occupantPet.DisplayName} to move aside.");
                return false;
            }
            return CompletePlayerStepOnto(level, targetX, targetY);
        }
        if (occupant != null)
        {
            player.IsSneaking = false; // attacking breaks Sneak
            player.LastAttackTurn = level.TurnNumber;

            // Design spec section 15: bumping into an unilluminated dark-room occupant must not
            // accidentally reveal its identity just because the player swung at it -- mirrors
            // ChaseAI's identical check for a monster attacking the player out of the dark.
            bool occupantVisible = level.Tiles[targetX, targetY].IsVisible;

            var result = player.PhysicalAttack(occupant, rng);
            AddStatusMessage(CombatMessages.Format(result, attackerIsPlayer: true, otherPartyVisible: occupantVisible));
            ApplyPoisonWeaponProc(result, occupant, level);
            AddStatusMessage(ItemEffectApplier.ApplyOnHitEffects(result, level, rng));

            // Dual Wield: an off-hand weapon fires a second, independent attack roll right
            // after the main-hand one, guarded on the target still being alive to hit.
            if (occupant.IsAlive && player.HasSkill(SkillCatalog.DualWield)
                && player.Equipment.Get(EquipmentSlot.OffHand)?.EquipmentCategory == EquipmentCategory.Weapon)
            {
                var offHandResult = player.PhysicalAttack(occupant, rng);
                AddStatusMessage(CombatMessages.Format(offHandResult, attackerIsPlayer: true, otherPartyVisible: occupantVisible));
                ApplyPoisonWeaponProc(offHandResult, occupant, level);
                AddStatusMessage(ItemEffectApplier.ApplyOnHitEffects(offHandResult, level, rng));
            }

            return true;
        }

        var door = level.GetDoorAt(targetX, targetY);
        if (door != null)
        {
            return HandleDoorBump(door);
        }

        var blockingRoomObject = level.GetRoomObjectAt(targetX, targetY);
        if (blockingRoomObject is { BlocksMovement: true })
        {
            AddStatusMessage($"The {blockingRoomObject.DisplayName} blocks the way.");
            return false;
        }

        if (!level.IsWalkable(targetX, targetY))
        {
            AddStatusMessage("That way is blocked.");
            return false;
        }

        return CompletePlayerStepOnto(level, targetX, targetY);
    }

    /// <summary>
    /// Pet and Companion System: a deliberate command distinct from bumping into the pet (which
    /// shoves it aside at random, toward safety when possible -- see DisplacePetOutOfTheWay). This
    /// instead trades tiles outright: the player takes the pet's exact tile, and the pet takes the
    /// player's. Routes through the same CompletePlayerStepOnto every other move uses, so swapping
    /// onto a trap/concealed item/Water/Ice tile behaves exactly like walking there normally --
    /// the player is genuinely relocating, not teleporting past the usual checks. No direction is
    /// read from input -- with only ever one pet today, "swap with the pet" needs no aiming.
    /// </summary>
    internal bool HandleSwapWithPet(Level level)
    {
        if (player.Pet is not { LifecycleState: PetLifecycleState.Active } pet || !level.Actors.Contains(pet))
        {
            AddStatusMessage("You have no pet nearby to swap places with.");
            return false;
        }

        if (Math.Max(Math.Abs(pet.X - player.X), Math.Abs(pet.Y - player.Y)) > 1)
        {
            AddStatusMessage($"{pet.DisplayName} is too far away to swap places with.");
            return false;
        }

        int petX = pet.X, petY = pet.Y;
        pet.MoveTo(player.X, player.Y);
        AddStatusMessage($"You swap places with {pet.DisplayName}.");
        return CompletePlayerStepOnto(level, petX, petY);
    }

    /// <summary>
    /// The shared back half of HandleMove: the destination has already been confirmed walkable
    /// and unoccupied (either by the normal walkable-tile check above, or -- Pet and Companion
    /// System -- because the player's own pet just stepped aside from it) -- everything from
    /// here on (water extinguish, trap reveal/trigger, concealed-item discovery, the dark-item
    /// notice, and the Water/Ice slip roll) is identical regardless of which path got here.
    /// </summary>
    private bool CompletePlayerStepOnto(Level level, int targetX, int targetY)
    {
        player.MoveTo(targetX, targetY);
        RecomputeFov();

        // Design spec section 10: walking onto Water immediately extinguishes any active,
        // water-vulnerable physical light source. Only ever finds something to extinguish on
        // the FIRST turn spent in the water -- see LightingSystem.ExtinguishForWater's own doc
        // comment on why that needs no extra "already tried" tracking.
        if (level.Tiles[targetX, targetY].FloorType == FloorType.Water)
        {
            var extinguishMessages = LightingSystem.ExtinguishForWater(player);
            if (extinguishMessages.Count > 0)
            {
                foreach (var extinguishMessage in extinguishMessages)
                {
                    AddStatusMessage(extinguishMessage);
                }
                RecomputeFov(); // a light just went out -- the dark-item-notice check right below needs fresh illumination, not what RecomputeFov computed a moment ago while the light was still lit
            }
        }

        RollDetectTraps(level);

        var trap = level.GetTrapAt(targetX, targetY);
        if (trap != null)
        {
            TriggerTrap(trap, level);
        }

        // Misplaced Items design doc: stepping onto a concealed item is a passive discovery
        // trigger, independent of the unrelated "trip over something" flavor check right below
        // (which only ever fires for ordinary, already-visible items sitting in the dark).
        foreach (var concealed in level.GetGroundItemsAt(targetX, targetY).Where(g => g.IsConcealed))
        {
            if (ItemDiscoveryRules.RollPassiveDiscovery(player, concealed, playerStandingOnTile: true, rng))
            {
                concealed.IsConcealed = false;
            }
        }

        // Design spec section 12: stepping onto an unseen item in an unilluminated dark room has
        // a chance to notice something without identifying it.
        var destinationTile = level.Tiles[targetX, targetY];
        if (destinationTile.IsDarkRoom && !destinationTile.IsIlluminated
            && level.GetItemsAt(targetX, targetY).Count > 0 && rng.NextDouble() < LightingConfig.DarkItemNoticeChance)
        {
            AddStatusMessage(DarkItemNoticeMessages[rng.Next(DarkItemNoticeMessages.Length)]);
        }

        // Prone/Knockdown System: rolled only here, right at the end of a successfully COMPLETED
        // move onto Water/Ice -- never for waiting, standing, teleporting, a blocked move, a
        // bump-attack, a door, or any other floor type (every one of those returns before this
        // point). A trap triggered a few lines up could have already killed the player -- IsAlive
        // guards against rolling for a corpse.
        if (player.IsAlive)
        {
            var floorType = destinationTile.FloorType;
            double baseChance = SlipConfig.BaseChanceFor(floorType);
            if (baseChance > 0)
            {
                bool feetOccupied = player.Equipment.Get(EquipmentSlot.Feet) != null;
                int adjustedLuck = player.Stats.Adjusted(PrimaryAttribute.Luck);
                if (rng.NextDouble() < SlipConfig.SlipChance(floorType, feetOccupied, adjustedLuck))
                {
                    KnockdownResolver.ApplyEnvironmentalFall(player);
                    AddStatusMessage(floorType == FloorType.Ice
                        ? "The ice slips beneath your feet, sending you crashing to the ground!"
                        : "Your feet slide in the water, and you fall hard!");
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Pet and Companion System: moves the pet off the tile the player is about to step onto,
    /// requiring a tile that won't hurt it (Safe Monster Spawning and Hazard-Aware Movement --
    /// ActorTerrainSafety, never a hazardous tile even as a last resort) and never onto the
    /// player's own tile. Free for both parties -- no scheduled turn/energy is consumed for the
    /// pet's own sidestep, matching how the player's own move is what triggers this in the first
    /// place, not an action the pet chose to take. Returns false when the pet is completely boxed
    /// in (every neighboring tile blocked, occupied, or hazardous), in which case the player
    /// simply can't push through.
    /// </summary>
    private bool DisplacePetOutOfTheWay(Pet pet, Level level)
    {
        var destination = FindPetSidestepTile(pet, level);
        if (destination == null)
        {
            return false;
        }

        pet.MoveTo(destination.Value.X, destination.Value.Y);
        return true;
    }

    private static readonly (int Dx, int Dy)[] SidestepDirections =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)
    };

    private (int X, int Y)? FindPetSidestepTile(Pet pet, Level level)
    {
        var candidates = new List<(int X, int Y)>();
        foreach (var (dx, dy) in SidestepDirections)
        {
            int x = pet.X + dx;
            int y = pet.Y + dy;
            // The player's own (about-to-be-vacated) tile isn't a valid sidestep spot -- the pet
            // would just be swapping into the exact tile the player is mid-step onto.
            if (x == player.X && y == player.Y)
            {
                continue;
            }
            if (!ActorTerrainSafety.CanActorOccupy(level, pet, x, y))
            {
                continue;
            }
            candidates.Add((x, y));
        }

        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
    }

    private static readonly string[] DarkItemNoticeMessages =
    {
        "You trip over something in the dark.",
        "Your boot strikes something unseen.",
        "Something shifts beneath your feet.",
        "You stumble over something in the darkness."
    };

    /// <summary>
    /// Stairs no longer trigger by merely stepping onto the tile -- '&lt;'/'&gt;' are explicit
    /// commands instead (see InputHandler), so a player can stand on a stairway to loot/fight
    /// without being swept to another floor by accident. Neither the wrong-direction case (on
    /// the other stairway) nor the no-stairs-here case consumes a turn -- only an actual floor
    /// change does. Internal (not private) so Diagnostics/SelfTest.cs can drive it directly,
    /// the same reasoning HandlePickUp/AwardDeathRewards were already bumped to internal for.
    /// </summary>
    internal bool HandleUseStairs(Level level, bool descending)
    {
        bool onStairsDown = (player.X, player.Y) == level.StairsDownPosition;
        bool onStairsUp = (player.X, player.Y) == level.StairsUpPosition;

        if (descending)
        {
            if (onStairsDown)
            {
                dungeonManager.Descend(player);
                RecomputeFov();
                AddStatusMessage($"You descend to floor {dungeonManager.CurrentLevel.FloorIndex}.");
                MovePetToCurrentFloor(level);
                return true;
            }
            if (onStairsUp)
            {
                AddStatusMessage("These stairs lead up, not down.");
                return false;
            }
            AddStatusMessage("There are no stairs here.");
            return false;
        }

        if (onStairsUp)
        {
            if (dungeonManager.CurrentFloorIndex == 1)
            {
                AddStatusMessage("You are already on the topmost floor.");
                return false;
            }
            dungeonManager.Ascend(player);
            RecomputeFov();
            AddStatusMessage($"You ascend to floor {dungeonManager.CurrentLevel.FloorIndex}.");
            MovePetToCurrentFloor(level);
            return true;
        }
        if (onStairsDown)
        {
            AddStatusMessage("These stairs lead down, not up.");
            return false;
        }
        AddStatusMessage("There are no stairs here.");
        return false;
    }

    /// <summary>Poison Weapon: while its window is still open, a successful player hit has a chance to also apply a poison DoT -- reuses the same ActiveEffect/TickDamage shape StatusEffect already produces for spell-inflicted Burning/Poisoned. Chance/duration/tick-damage all come from the Ability Proficiency System's bespoke Poison Weapon table for the player's current rank -- see ProficiencyScaling.PoisonWeaponProfile.</summary>
    private void ApplyPoisonWeaponProc(AttackResult attack, Actor target, Level level)
    {
        if (!attack.Hit || !target.IsAlive || level.TurnNumber >= player.PoisonWeaponUntilTurn)
        {
            return;
        }

        var rank = player.RankOf(SkillCatalog.PoisonWeapon.ProficiencyId);
        var profile = ProficiencyScaling.PoisonWeaponProfile(rank);
        if (rng.NextDouble() >= profile.Chance)
        {
            return;
        }

        target.ActiveEffects.Add(new ActiveEffect("Poison Weapon", level.TurnNumber + profile.Duration)
        {
            TickDamage = profile.TickDamage,
            TickDamageType = DamageType.Poison,
            DamageSourceDescription = $"{CombatMessages.WithArticle(player.Name)}'s poisoned blade",
            Owner = player
        });
    }

    /// <summary>Reveals (cosmetic only -- doesn't disarm) any trap within a small radius of the player's new position -- called from RollDetectTraps on a successful roll.</summary>
    private void RevealNearbyTraps(Level level)
    {
        foreach (var trap in level.Traps.Where(t => !t.IsRevealed && !t.IsTriggered))
        {
            int dx = trap.X - player.X;
            int dy = trap.Y - player.Y;
            if ((dx * dx) + (dy * dy) <= TrapRevealRadius * TrapRevealRadius)
            {
                trap.IsRevealed = true;
            }
        }
    }

    private const double DetectTrapsBaseChance = 0.5;
    private const double DetectTrapsScalingPerPoint = 0.03;
    private const double DetectTrapsMinChance = 0.10;
    private const double DetectTrapsMaxChance = 0.95;
    private const int DetectTrapsBaselineStat = 10;

    /// <summary>Same base-chance/scaling/clamp shape as StatBasedIdentifyEffect.ChanceForStat -- exposed for Diagnostics/SelfTest.cs as a pure function of the adjusted stat value.</summary>
    internal static double DetectTrapsChanceForStat(int adjustedStat) =>
        Math.Clamp(DetectTrapsBaseChance + (adjustedStat - DetectTrapsBaselineStat) * DetectTrapsScalingPerPoint, DetectTrapsMinChance, DetectTrapsMaxChance);

    /// <summary>Detect Traps (Thief skill / Mage spell): while player.DetectTrapsUntilTurn hasn't expired, each move rolls a DetectTrapsStat-scaled chance to reveal nearby traps -- see DetectTrapsEffect. The window being open and a given roll actually succeeding are different states -- it can simply fail on any given move, so this only calls RevealNearbyTraps on success. Rank applies the standard utility percentage-point adjustment, keyed off whichever of the two abilities is actually open (Agility means the Thief skill; Knowledge means the Mage spell).</summary>
    internal void RollDetectTraps(Level level)
    {
        if (level.TurnNumber >= player.DetectTrapsUntilTurn)
        {
            return;
        }

        string proficiencyId = player.DetectTrapsStat == PrimaryAttribute.Agility
            ? SkillCatalog.DetectTraps.ProficiencyId
            : SpellCatalog.DetectTraps.ProficiencyId;
        var rank = player.RankOf(proficiencyId);
        double chance = DetectTrapsChanceForStat(player.Stats.Adjusted(player.DetectTrapsStat)) + ProficiencyScaling.PercentagePointAdjustment(rank);
        if (rng.NextDouble() < chance)
        {
            RevealNearbyTraps(level);
        }
    }

    private void TriggerTrap(Trap trap, Level level)
    {
        trap.IsTriggered = true;
        int preDamageHealth = player.Health?.Current ?? 0;
        CombatStatsTracker.ApplyDamage(player, trap.Damage, owner: null);
        player.LastDamageSource = "a hidden trap";
        player.LastDamageOwner = null; // environmental -- see EnvironmentalFloorEffects.Apply
        string severityWord = CombatMessages.SeverityWord(CombatMessages.ClassifySeverity(trap.Damage, preDamageHealth));
        AddStatusMessage($"A hidden trap triggers! You take {severityWord} damage.");
    }

    /// <summary>
    /// Mirrors HandlePickUp's "act on whatever's at the player's own tile" pattern rather than a
    /// directional prompt, same as items. A Chest is now a persistent container (Persistent
    /// Containers proposal) rather than a one-shot spill-onto-the-floor object: interacting with
    /// a closed (and unlocked, or successfully picked) chest opens it and shows
    /// ContainerScreen; interacting again with an already-open chest (without the screen open)
    /// closes it instead. Both directions of this toggle consume a turn; leaving the screen via
    /// Esc is free and leaves IsOpen exactly as it was set here.
    /// </summary>
    /// <summary>
    /// `showScreen` exists purely as a test seam (see Diagnostics/SelfTest.cs) so a test exercising
    /// the open/close toggle never blocks on ContainerScreen's interactive Console.ReadKey loop --
    /// real gameplay always calls this with the default. Internal (not private) for the same reason
    /// HandlePickUp's own offerEquip parameter is. Corpse System section 10: generalizes the chest-
    /// only flow into "choose a container on this tile" -- a chest and one or more loot-bearing
    /// corpses sharing a tile prompt for which to open first; several corpses with no chest prompt
    /// among just those; exactly one valid container (of either kind) opens directly.
    /// </summary>
    internal bool HandleOpenChest(Level level, bool showScreen = true)
    {
        var chest = level.GetChestAt(player.X, player.Y);
        var corpses = level.GetCorpsesAt(player.X, player.Y).Where(c => c.Container.Contents.Items.Count > 0).ToList();

        if (chest == null && corpses.Count == 0)
        {
            AddStatusMessage("There's nothing here to open.");
            return false;
        }

        if (chest != null && corpses.Count > 0)
        {
            var labels = new List<string> { "Chest" };
            labels.AddRange(CorpseSelectionLabels(corpses));
            int? choice = MenuPrompt.Choose("Open which container?", labels, allowCancel: true);
            if (choice == null)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }
            return choice.Value == 0 ? OpenChest(chest, level, showScreen) : OpenCorpse(corpses[choice.Value - 1], level, showScreen);
        }

        if (chest != null)
        {
            return OpenChest(chest, level, showScreen);
        }

        if (corpses.Count == 1)
        {
            return OpenCorpse(corpses[0], level, showScreen);
        }

        int? corpseChoice = MenuPrompt.Choose("Open which corpse?", CorpseSelectionLabels(corpses), allowCancel: true);
        if (corpseChoice == null)
        {
            AddStatusMessage("Cancelled.");
            return false;
        }
        return OpenCorpse(corpses[corpseChoice.Value], level, showScreen);
    }

    private bool OpenChest(Chest chest, Level level, bool showScreen)
    {
        if (chest.IsOpen)
        {
            chest.IsOpen = false;
            AddStatusMessage("You close the chest.");
            return true;
        }

        if (chest.IsLocked)
        {
            if (!player.HasSkill(SkillCatalog.PickLock))
            {
                AddStatusMessage("You don't know how to pick locks.");
                return false;
            }

            var lockpicks = player.Inventory.Items.FirstOrDefault(i => i.Type == ItemType.Lockpick && i.Charges > 0);
            if (lockpicks == null)
            {
                AddStatusMessage("You need a set of lockpicks to pick this lock.");
                return false;
            }

            int roll = rng.Next(1, 21) + player.Stats.Adjusted(PrimaryAttribute.Knowledge) / 2
                + ProficiencyScaling.D20Adjustment(player.RankOf(SkillCatalog.PickLock.ProficiencyId));
            if (roll < chest.Difficulty)
            {
                BreakLockpickOnFailure(lockpicks);
                return true;
            }

            chest.IsLocked = false;
            chest.IsOpen = true;
            AddStatusMessage("You pick the lock and open the chest.");
            if (showScreen)
            {
                ContainerScreen.Show(player, chest.Container, "Chest", level);
            }
            return true;
        }

        chest.IsOpen = true;
        AddStatusMessage("You open the chest.");
        if (showScreen)
        {
            ContainerScreen.Show(player, chest.Container, "Chest", level);
        }
        return true;
    }

    /// <summary>Corpse System section 10: opens a loot-bearing corpse's remove-only container screen (never a put option), then -- if the player emptied it during that visit -- converts it into a portable corpse item on the same tile (section 6).</summary>
    private bool OpenCorpse(Corpse corpse, Level level, bool showScreen)
    {
        string corpseName = CorpseItemFactory.FormatName(corpse.Metadata);
        AddStatusMessage($"You search the {corpseName}.");
        if (showScreen)
        {
            ContainerScreen.Show(player, corpse.Container, char.ToUpper(corpseName[0]) + corpseName[1..], level, allowPut: false);
            ConvertCorpseIfEmptied(corpse, level);
        }
        return true;
    }

    /// <summary>Corpse System section 6: the moment a corpse's last item is removed, it stops being an openable container and becomes a portable item on the same tile -- checked once the player leaves its screen (the only point removal can actually have happened) rather than hooking into ContainerTransferService itself, which chests/bags also share and shouldn't need to know corpses exist.</summary>
    internal void ConvertCorpseIfEmptied(Corpse corpse, Level level)
    {
        if (corpse.Container.Contents.Items.Count > 0)
        {
            return;
        }

        level.Corpses.Remove(corpse);
        var portableItem = CorpseItemFactory.CreatePortableItem(corpse.Metadata);
        level.AddVisibleItem(corpse.X, corpse.Y, portableItem, ItemLandingOrigin.Generated);
        AddStatusMessage("You empty the corpse. The remains can now be picked up.");
    }

    /// <summary>"corpse of a giant rat -- 2 items" -- appends a "(2)"-style distinguishing number, menu-local only (never part of the item's own permanent name), when several selectable corpses would otherwise show an identical label (proposal section 9).</summary>
    internal static List<string> CorpseSelectionLabels(IReadOnlyList<Corpse> corpses)
    {
        var baseNames = corpses.Select(c => CorpseItemFactory.FormatName(c.Metadata)).ToList();
        var labels = new List<string>();
        for (int i = 0; i < corpses.Count; i++)
        {
            string baseName = baseNames[i];
            bool needsOrdinal = baseNames.Count(n => n == baseName) > 1;
            int ordinal = baseNames.Take(i + 1).Count(n => n == baseName);
            int itemCount = corpses[i].Container.Contents.Items.Count;
            string name = needsOrdinal ? $"{baseName} ({ordinal})" : baseName;
            labels.Add($"{name} -- {itemCount} item{(itemCount == 1 ? "" : "s")}");
        }
        return labels;
    }

    /// <summary>
    /// Bumping into a locked Door offers every method currently available (pick/bash/key),
    /// auto-picking the one option when only one applies, and prompting when several do.
    /// Once bashing is already underway (Health has been chipped), subsequent bumps just
    /// continue bashing without re-prompting -- you've already committed to that approach.
    /// Internal (not private) so Diagnostics/SelfTest.cs can exercise the single-option (no
    /// MenuPrompt) path directly -- see the same reasoning HandlePickUp/PickUpAll already use.
    /// </summary>
    internal bool HandleDoorBump(Door door)
    {
        if (!door.IsLocked)
        {
            door.IsOpen = true;
            AddStatusMessage("You open the door.");
            return true;
        }

        bool canPick = door.IsPickable && player.HasSkill(SkillCatalog.PickLock)
            && player.Inventory.Items.Any(i => i.Type == ItemType.Lockpick && i.Charges > 0);
        bool canBash = door.IsBashable;
        bool hasKey = player.Inventory.Items.Any(i => i.IsSkeletonKey && (i.Charges ?? 1) > 0);

        if (canBash && door.Health < door.MaxHealth)
        {
            return BashDoor(door);
        }

        var labels = new List<string>();
        if (canPick) labels.Add("Pick the lock");
        if (canBash) labels.Add("Bash it down");
        if (hasKey) labels.Add("Use a skeleton key");

        if (labels.Count == 0)
        {
            AddStatusMessage("The door is locked, and you have no way through.");
            return false;
        }

        string choice = labels[0];
        if (labels.Count > 1)
        {
            int? index = MenuPrompt.Choose("The door is locked. How do you want to get through?", labels, allowCancel: true);
            if (index == null)
            {
                AddStatusMessage("Cancelled.");
                return false;
            }
            choice = labels[index.Value];
        }

        return choice switch
        {
            "Pick the lock" => PickDoorLock(door),
            "Bash it down" => BashDoor(door),
            "Use a skeleton key" => UseSkeletonKeyOnDoor(door),
            _ => false
        };
    }

    private bool PickDoorLock(Door door)
    {
        var lockpicks = player.Inventory.Items.FirstOrDefault(i => i.Type == ItemType.Lockpick && i.Charges > 0);
        if (lockpicks == null)
        {
            AddStatusMessage("You need a set of lockpicks to pick this lock.");
            return false;
        }

        int roll = rng.Next(1, 21) + player.Stats.Adjusted(PrimaryAttribute.Knowledge) / 2
            + ProficiencyScaling.D20Adjustment(player.RankOf(SkillCatalog.PickLock.ProficiencyId));
        if (roll < door.Difficulty)
        {
            BreakLockpickOnFailure(lockpicks);
            return true;
        }

        door.IsLocked = false;
        door.IsOpen = true;
        AddStatusMessage("You pick the lock and open the door.");
        return true;
    }

    /// <summary>Only ever called after a failed pick attempt -- a successful pick never risks the set. Removes the whole item once its last charge breaks. Break chance comes from the Ability Proficiency System's bespoke Pick Lock table for the player's current rank -- see ProficiencyScaling.PickLockBreakChance.</summary>
    private void BreakLockpickOnFailure(Item lockpicks)
    {
        double breakChance = ProficiencyScaling.PickLockBreakChance(player.RankOf(SkillCatalog.PickLock.ProficiencyId));
        if (rng.NextDouble() >= breakChance)
        {
            AddStatusMessage("You fail to pick the lock.");
            return;
        }

        lockpicks.Charges--;
        if (lockpicks.Charges <= 0)
        {
            player.Inventory.RemoveItem(lockpicks);
            AddStatusMessage("You fail to pick the lock, and your last lockpick snaps!");
        }
        else
        {
            AddStatusMessage($"You fail to pick the lock, and a pick snaps. ({lockpicks.Charges} left)");
        }
    }

    /// <summary>No skill required -- anyone can ram a bashable door, same as anyone can bump-attack a monster. Chips away at Health across multiple bumps rather than an instant break.</summary>
    private bool BashDoor(Door door)
    {
        int damage = Math.Max(1, player.BasePhysicalAttackPower - 2);
        door.Health -= damage;

        if (door.Health <= 0)
        {
            door.IsLocked = false;
            door.IsOpen = true;
            AddStatusMessage("You smash the door off its hinges!");
        }
        else
        {
            AddStatusMessage($"You bash the door. ({Math.Max(0, door.Health)}/{door.MaxHealth} HP)");
        }
        return true;
    }

    private bool UseSkeletonKeyOnDoor(Door door)
    {
        var key = player.Inventory.Items.First(i => i.IsSkeletonKey);
        door.IsLocked = false;
        door.IsOpen = true;

        // Decrement-not-remove, same shape as a Lockpick set losing one pick -- only the very
        // last charge actually crumbles the key away. Charges is never null here in practice
        // (Items.SkeletonKey always spawns with charges: 1), but the null-coalescing default
        // keeps this correct even for a pre-existing save's key from before this existed.
        key.Charges = (key.Charges ?? 1) - 1;
        if (key.Charges <= 0)
        {
            player.Inventory.RemoveItem(key);
            AddStatusMessage("You unlock the door with your last skeleton key. It crumbles to dust.");
        }
        else
        {
            AddStatusMessage($"You unlock the door with a skeleton key. It crumbles to dust, leaving {key.Charges} more in your collection.");
        }
        return true;
    }

    /// <summary>Internal (not private) so Diagnostics/SelfTest.cs can exercise illumination-triggered passive discovery deterministically -- see CheckIlluminationDiscoveries.</summary>
    internal void RecomputeFov()
    {
        // Illumination must be current BEFORE FOV gates a dark room's tiles by it -- see
        // FieldOfView.Compute's own doc comment on this ordering requirement.
        var level = dungeonManager.CurrentLevel;
        LightingSystem.RecomputeIllumination(level);
        FieldOfView.Compute(level, player.X, player.Y, ViewRadius);
        CheckIlluminationDiscoveries(level);
    }

    /// <summary>
    /// Misplaced Items design doc: "the player first illuminates a tile containing a concealed
    /// item" is a one-shot passive discovery trigger -- GroundItem.ConsideredForIlluminationDiscovery
    /// gates it so a tile that stays lit for many turns only ever gets this one attempt, never a
    /// fresh roll on every RecomputeFov call (which runs on nearly every action). Only considers
    /// currently-visible tiles, not the whole level, so this stays cheap and never "notices"
    /// something the player has no way to have actually seen light reach.
    /// </summary>
    private void CheckIlluminationDiscoveries(Level level)
    {
        foreach (var groundItem in level.GroundItems)
        {
            if (!groundItem.IsConcealed || groundItem.ConsideredForIlluminationDiscovery)
            {
                continue;
            }

            var tile = level.Tiles[groundItem.X, groundItem.Y];
            if (!tile.IsVisible || !tile.IsIlluminated)
            {
                continue;
            }

            groundItem.ConsideredForIlluminationDiscovery = true;
            bool standing = groundItem.X == player.X && groundItem.Y == player.Y;
            if (ItemDiscoveryRules.RollPassiveDiscovery(player, groundItem, standing, rng))
            {
                groundItem.IsConcealed = false;
            }
        }
    }

    /// <summary>
    /// Folds the wall-clock time elapsed since sessionStartUtc into Player.AdventureRecord.
    /// TimeInDungeonSeconds and resets sessionStartUtc back to now -- called on quit (before
    /// SaveManager.Save, so the persisted total already includes this session), on death (so the
    /// final Adventure Record shows an accurate, permanently-stopped total), and whenever the
    /// player opens the Adventure Record screen mid-run (so a live check shows an up-to-date
    /// total instead of only ever reflecting previously-completed sessions). Quitting and later
    /// loading naturally pauses/resumes the clock: nothing runs between a quit's fold-in and the
    /// next GameLoop's own sessionStartUtc field initializer capturing the moment that load
    /// happens, so time spent with the save closed is never counted.
    /// </summary>
    internal void RecordElapsedDungeonTime()
    {
        var now = DateTime.UtcNow;
        player.AdventureRecord.TimeInDungeonSeconds += (long)(now - sessionStartUtc).TotalSeconds;
        sessionStartUtc = now;
    }

    /// <summary>
    /// Awards XP/gold/loot for any monster that just died, before RemoveDeadActors
    /// sweeps it away. Lives here rather than in combat/damage code (bump
    /// attacks, spell effects) so those never need to know a reward formula
    /// exists -- this is the one place death is turned into a reward. Each
    /// dead monster is now its own status message rather than being appended
    /// onto whatever message preceded it: with a 2-message history, showing
    /// "you hit it" and "it dies" as separate entries reads better than one
    /// run-on sentence, and multiple simultaneous deaths (an AoE kill)
    /// naturally surface the most recent ones. Ground items can now stack
    /// freely on one tile, so loot just lands directly on the death tile --
    /// no more spreading a second drop onto an adjacent tile.
    /// </summary>
    internal void AwardDeathRewards(Level level)
    {
        foreach (var monster in level.Actors.OfType<Monster>().Where(m => !m.IsAlive))
        {
            // Only a death the player actually caused (directly, via a delayed effect they
            // applied -- see Actor.LastDamageOwner -- or via their own pet, resolved through the
            // Pet and Companion System's ownership chain) earns XP/gold; a monster that died to
            // environmental damage, a trap, or another monster still drops its loot below, it
            // just doesn't reward the player for a kill that wasn't theirs.
            bool playerCredited = Allegiance.ResolveRewardBeneficiary(monster.LastDamageOwner) is Player;
            long xp = 0;
            long gold = 0;
            int previousLevel = player.Level;
            if (playerCredited)
            {
                xp = ExperienceRewardCalculator.CalculateEnemyExperience(monster, player, dungeonManager.CurrentFloorIndex);
                gold = GoldDropCalculator.CalculateGoldDrop(monster, rng);
                if (monster.IsBoss)
                {
                    // Reuses the normal gold calculation entirely rather than a separate boss
                    // formula -- just doubles the result.
                    gold = (long)(gold * BossConfig.BossGoldMultiplier);
                }

                player.AddExperience(xp);
                player.CollectGold(gold);

                // Adventure Record: a credited kill, alongside the XP/gold reward it's already
                // earning above -- same one-time guarantee (this loop only ever sees a given
                // monster once, since RemoveDeadActors drops it from level.Actors immediately
                // after this method returns each turn).
                player.AdventureRecord.MonstersKilled++;
                if (monster.IsBoss)
                {
                    player.AdventureRecord.BossesKilled++;
                    if (monster.Level > player.AdventureRecord.HighestLevelBossLevel)
                    {
                        player.AdventureRecord.HighestLevelBossLevel = monster.Level;
                        player.AdventureRecord.HighestLevelBossName = monster.DisplayName;
                    }
                }
            }

            // A monster that spawned wearing/wielding gear (see Monster.CreateRandom's
            // equip step) drops exactly that on death instead of an unrelated random
            // roll -- what you saw it carrying is what you get. Only a monster with
            // nothing equipped falls back to the random loot table, which (by design,
            // via NoDropChance) may still leave nothing at all -- a drop was never
            // guaranteed and still isn't for this case.
            var loot = monster.Equipment.AllEquipped.Any()
                ? monster.Equipment.AllEquipped.Select(kvp => kvp.Value).ToList()
                : LootGenerator.GenerateLoot(monster, player, rng);

            // Pick Pocket: a chance at one more roll of the drop table on top of the loot
            // above -- reuses GenerateLoot rather than a bespoke bonus-item API. Applies
            // either way, as an extra find beyond whatever the monster was carrying. Chance
            // comes from the Ability Proficiency System's bespoke Pick Pocket table for the
            // player's current rank -- see ProficiencyScaling.PickPocketBonusChance.
            if (player.HasSkill(SkillCatalog.PickPocket)
                && rng.NextDouble() < ProficiencyScaling.PickPocketBonusChance(player.RankOf(SkillCatalog.PickPocket.ProficiencyId)))
            {
                var bonusLoot = LootGenerator.GenerateLoot(monster, player, rng);
                if (bonusLoot.Count > 0)
                {
                    loot.Add(bonusLoot[0]);
                }
            }

            // Anything still sitting unused in the monster's own Inventory (a potion it
            // never got hurt enough to drink -- see ChaseAI's healing-item check) drops
            // too, on top of whatever else it drops. Nothing it was actually carrying
            // should just vanish, regardless of which branch above produced `loot`.
            loot.AddRange(monster.Inventory.Items);

            // Corpse System: loot no longer lands loose on the ground -- it goes inside a corpse
            // (or, if nothing survived Fire/Lava, straight into an immediately portable corpse
            // item). See DeathDropService's own doc comment.
            var metadata = CorpseMetadataFactory.ForMonster(monster);
            var (corpse, lootDestructionMessages) = DeathDropService.CreateCorpse(level, monster.X, monster.Y, metadata, loot);

            string message = playerCredited
                ? $"{CombatMessages.Label(monster, capitalized: true)} dies! (+{xp} XP, +{gold} gold)"
                : $"{CombatMessages.Label(monster, capitalized: true)} dies!";
            if (player.Level > previousLevel)
            {
                message += $" You are now level {player.Level}!";
            }
            message += corpse != null ? " It leaves behind a corpse." : " It leaves behind an empty corpse.";
            if (lootDestructionMessages.Count > 0)
            {
                message += " " + string.Join(" ", lootDestructionMessages);
            }

            AddStatusMessage(message);
        }
    }

    /// <summary>Places the player's pet next to them the first time a level is entered -- a freshly created character's pet has no valid position yet (still (0,0)), and a restored-but-somehow-invalid saved position falls back the same way. A pet with an already-valid saved position (the ordinary resumed-save case) is left exactly where it was.</summary>
    internal void PlacePetIfNeeded()
    {
        if (player.Pet is not { LifecycleState: PetLifecycleState.Active } pet)
        {
            return;
        }

        var level = dungeonManager.CurrentLevel;
        if (level.Actors.Contains(pet))
        {
            return;
        }

        if (!level.IsWalkable(pet.X, pet.Y) || level.GetActorAt(pet.X, pet.Y) != null)
        {
            // Safe Monster Spawning and Hazard-Aware Movement: automatic pet placement prefers a
            // hazard-free tile, falling back to merely-unblocked only if nothing safe is adjacent
            // -- unlike a spawn, a pet can't simply be skipped, so it must land somewhere.
            var spot = ActorTerrainSafety.FindSafeAdjacentTile(level, pet, player.X, player.Y)
                ?? level.FindFreeAdjacentTile(player.X, player.Y) ?? (player.X, player.Y);
            pet.MoveTo(spot.X, spot.Y);
        }

        level.Actors.Add(pet);
        level.Scheduler.Register(pet);
    }

    /// <summary>
    /// Pet and Companion System section 12: a living pet follows its owner through stairs.
    /// `oldLevel` is the level the player was JUST on -- HandleUseStairs's own `level` parameter,
    /// captured before DungeonManager.Descend/Ascend switched CurrentLevel out from under it. A
    /// pet awaiting respawn has nothing to move (it isn't registered on any level); its deadline
    /// keeps counting down against the owner's global TurnCount regardless of which floor that
    /// lands on.
    /// </summary>
    internal void MovePetToCurrentFloor(Level oldLevel)
    {
        if (player.Pet is not { LifecycleState: PetLifecycleState.Active } pet || !oldLevel.Actors.Contains(pet))
        {
            return;
        }

        oldLevel.Actors.Remove(pet);
        oldLevel.Scheduler.Unregister(pet);

        var newLevel = dungeonManager.CurrentLevel;
        // Safe Monster Spawning and Hazard-Aware Movement -- see PlacePetIfNeeded's identical reasoning.
        var spot = ActorTerrainSafety.FindSafeAdjacentTile(newLevel, pet, player.X, player.Y)
            ?? newLevel.FindFreeAdjacentTile(player.X, player.Y) ?? (player.X, player.Y);
        pet.MoveTo(spot.X, spot.Y);
        newLevel.Actors.Add(pet);
        newLevel.Scheduler.Register(pet);

        // Old-floor combat targets/threats no longer make sense on the new floor.
        pet.PreferredTarget = null;
        pet.ThreatMemory.Clear();

        AddStatusMessage("Your pet dog has tracked you down and rejoins the fight.");
    }

    /// <summary>Pet and Companion System section 10: a dead pet is removed from the level/scheduler (Level.RemoveDeadActors, called right after this, already handles that generically for any non-player actor) and marked awaiting respawn -- never awarded rewards, never drops generated loot. Corpse System section 13: it still leaves a corpse (empty unless the pet happened to be carrying something -- pets never do today, but the check is generic); the corpse is fully independent of the owner-to-pet relationship, so a later replacement pet arriving is completely unaffected by it.</summary>
    internal void HandlePetDeath(Level level)
    {
        if (player.Pet is not { LifecycleState: PetLifecycleState.Active } pet || pet.IsAlive)
        {
            return;
        }

        var metadata = CorpseMetadataFactory.ForPet(pet);
        DeathDropService.CreateCorpse(level, pet.X, pet.Y, metadata, pet.Inventory.Items.ToList());

        pet.LifecycleState = PetLifecycleState.AwaitingRespawn;
        pet.RespawnAtOwnerTurn = player.TurnCount + PetConfig.RespawnDelayOwnerTurns;
        pet.PreferredTarget = null;
        pet.ThreatMemory.Clear();

        bool wasVisible = level.Tiles[pet.X, pet.Y].IsVisible;
        if (wasVisible)
        {
            AddStatusMessage($"{CombatMessages.Label(pet, capitalized: true)} falls, but may yet return.");
        }
    }

    /// <summary>Pet and Companion System section 11: revives the same Pet instance in place (see Pet's own doc comment on why identity is never replaced) once its respawn delay has elapsed.</summary>
    internal void RespawnPet(Pet pet)
    {
        var level = dungeonManager.CurrentLevel;
        var spot = FindPetRespawnTile(level, pet);
        pet.MoveTo(spot.X, spot.Y);

        PetProgression.SyncToLevel(pet, player.Level);
        pet.Health.SetCurrent(pet.Health.Max);
        pet.ActiveEffects.Clear();
        pet.StunnedUntilTurn = 0;
        pet.SilencedUntilTurn = 0;
        pet.FrightenedUntilTurn = 0;
        pet.CcImmuneUntilTurn = 0;
        pet.IsProne = false;
        pet.PreferredTarget = null;
        pet.ThreatMemory.Clear();
        pet.LifecycleState = PetLifecycleState.Active;

        level.Actors.Add(pet);
        level.Scheduler.Register(pet);

        AddStatusMessage("A new pet dog has tracked you down to join the fight.");
    }

    private const int PetRespawnMinDistanceSquared = 36; // "several tiles away" -- 6 tiles, per the proposal's own recommendation

    /// <summary>
    /// Pet and Companion System section 11: prefers a reachable, unoccupied, unseen tile several
    /// tiles from the owner; progressively relaxes (drop the unseen requirement, then the
    /// distance requirement) rather than ever postponing the respawn for an unavailable ideal
    /// spot; falls back to a tile directly adjacent to the owner as a last resort.
    /// </summary>
    internal (int X, int Y) FindPetRespawnTile(Level level, Pet pet)
    {
        var candidates = new List<(int X, int Y)>();
        for (int x = 0; x < level.Width; x++)
        {
            for (int y = 0; y < level.Height; y++)
            {
                // Safe Monster Spawning and Hazard-Aware Movement: ActorTerrainSafety replaces the
                // old hard-coded Lava/Fire literal check here, so this stays in sync with the one
                // centralized hazard rule instead of maintaining its own copy.
                if (level.IsBlockedForActorMovement(x, y) || !level.Tiles[x, y].IsReachable
                    || level.GetTrapAt(x, y) != null || (x, y) == level.StairsUpPosition || (x, y) == level.StairsDownPosition
                    || !ActorTerrainSafety.IsSafeForActor(level, pet, x, y))
                {
                    continue;
                }
                candidates.Add((x, y));
            }
        }

        int DistanceSq((int X, int Y) tile) => ((tile.X - player.X) * (tile.X - player.X)) + ((tile.Y - player.Y) * (tile.Y - player.Y));

        var farUnseen = candidates.Where(c => !level.Tiles[c.X, c.Y].IsVisible && DistanceSq(c) >= PetRespawnMinDistanceSquared).ToList();
        if (farUnseen.Count > 0)
        {
            return farUnseen[rng.Next(farUnseen.Count)];
        }

        var far = candidates.Where(c => DistanceSq(c) >= PetRespawnMinDistanceSquared).ToList();
        if (far.Count > 0)
        {
            return far[rng.Next(far.Count)];
        }

        if (candidates.Count > 0)
        {
            return candidates.OrderByDescending(DistanceSq).First();
        }

        // The whole-level scan above found nothing at all (every reachable tile hazardous) --
        // an extreme edge case; still prefer a safe spot next to the owner before giving up
        // entirely on hazard-awareness for this one placement.
        return ActorTerrainSafety.FindSafeAdjacentTile(level, pet, player.X, player.Y)
            ?? level.FindFreeAdjacentTile(player.X, player.Y) ?? (player.X, player.Y);
    }

    private void HandleDeath()
    {
        // Stops the clock the instant death is confirmed -- see RecordElapsedDungeonTime's own
        // doc comment. Everything after this point (the graveyard record, the death screen, the
        // optional final Adventure Record) reads a permanently frozen total.
        RecordElapsedDungeonTime();

        // Permadeath enforcement: delete the save the instant death is confirmed,
        // before showing anything else, so a crash mid-death-screen can't leave
        // a reloadable save behind.
        SaveManager.DeleteSave();

        string causeOfDeath = string.IsNullOrEmpty(player.LastDamageSource) ? "unknown causes" : player.LastDamageSource;

        GraveyardManager.Add(new DeadCharacterRecord
        {
            Name = player.Name,
            ClassName = player.Class.Name,
            RaceName = player.Race.Name,
            Level = player.Level,
            TurnCount = player.TurnCount,
            FloorReached = dungeonManager.CurrentFloorIndex,
            DiedAtUtc = DateTime.UtcNow,
            CauseOfDeath = causeOfDeath
        });

        Renderer.RenderMessage(
            $"You have died on floor {dungeonManager.CurrentFloorIndex}.\n" +
            $"Killed by {causeOfDeath}.\n" +
            $"Level {player.Level}, {player.TurnCount} turns taken.\n\n" +
            "Permadeath: your save has been deleted.\n\n" +
            "View your Adventure Record? (Y/N)");

        ConsoleKey choice;
        do
        {
            choice = Console.ReadKey(true).Key;
        } while (choice != ConsoleKey.Y && choice != ConsoleKey.N);

        if (choice == ConsoleKey.Y)
        {
            AdventureRecordScreen.Show(player, finalView: true);
        }
    }
}
