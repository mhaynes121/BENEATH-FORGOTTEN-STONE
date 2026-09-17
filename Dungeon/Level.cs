using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

public class Level
{
    public int FloorIndex { get; }
    public int Width { get; }
    public int Height { get; }
    public Tile[,] Tiles { get; }
    public List<Actor> Actors { get; } = new();
    public List<GroundItem> GroundItems { get; } = new();
    public List<Chest> Chests { get; } = new();

    /// <summary>Corpse System: loot-bearing corpses only -- an emptied corpse becomes a portable Item (see CorpseItemFactory) and moves into GroundItems instead, so this list and GroundItems never overlap for the same physical corpse. Never affects IsBlockedForActorMovement/IsBlockedForObjectPlacement -- corpses never block anything.</summary>
    public List<Corpse> Corpses { get; } = new();
    public List<Trap> Traps { get; } = new();
    public List<Door> Doors { get; } = new();
    public List<RoomObject> RoomObjects { get; } = new();

    /// <summary>Real, fixed environmental sound sources rolled at generation time for a subset of special-floor rooms -- see DungeonGenerator.AssignSpecialFloors and Core/SoundSystem.cs.</summary>
    public List<AmbientSoundSource> AmbientSoundSources { get; } = new();

    /// <summary>Per-level ambient-sound bookkeeping (cooldown, silence timer, recent-sound memory) -- fresh for every newly constructed Level, so a level visited for the first time always starts quiet. See DungeonSoundState's own doc comment for why this is deliberately not persisted across save/load.</summary>
    public DungeonSoundState SoundState { get; } = new();

    /// <summary>
    /// Each Level owns its own scheduler. GameLoop only ever calls
    /// GetNextActor() on the *current* level's scheduler, so any level
    /// that isn't active simply never ticks -- that's how off-screen
    /// floors stay "frozen" under Approach A.
    /// </summary>
    public TurnScheduler Scheduler { get; } = new();

    public (int X, int Y) StairsUpPosition { get; set; }
    public (int X, int Y) StairsDownPosition { get; set; }

    /// <summary>Advances once per completed player action. Backs spell cooldowns and buff/DoT durations -- see EffectProcessor.</summary>
    public int TurnNumber { get; private set; }

    public void AdvanceTurn() => TurnNumber++;

    /// <summary>Sets TurnNumber directly -- used only when reconstructing a saved floor, where AdvanceTurn's increment-by-one semantics don't apply.</summary>
    public void RestoreTurnNumber(int value) => TurnNumber = value;

    public Level(int floorIndex, int width, int height)
    {
        FloorIndex = floorIndex;
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Tiles[x, y] = Tile.CreateWall();
            }
        }
    }

    public bool IsInBounds(int x, int y) => x >= 0 && x < Width && y >= 0 && y < Height;

    public bool IsWalkable(int x, int y) => IsInBounds(x, y) && Tiles[x, y].IsWalkable;

    /// <summary>
    /// Tile transparency, but also opaque through a closed Door or a vision-blocking RoomObject
    /// -- the Tile itself is always plain floor underneath either (both are overlays, never a
    /// Tile mutation), so FOV/line-of-sight callers need this instead of reading
    /// Tiles[x,y].IsTransparent directly, or a closed door/boulder would be invisible to
    /// shadowcasting and let you see straight through it. Automatically stops blocking the
    /// moment a door opens (GetDoorAt returns null then); a RoomObject's BlocksVision is fixed
    /// for its lifetime, so that half never changes on its own.
    /// </summary>
    public bool IsTransparent(int x, int y) =>
        IsInBounds(x, y) && Tiles[x, y].IsTransparent && GetDoorAt(x, y) == null
        && GetRoomObjectAt(x, y) is not { BlocksVision: true };

    public RoomObject GetRoomObjectAt(int x, int y) => RoomObjects.FirstOrDefault(o => o.X == x && o.Y == y);

    /// <summary>
    /// True if an ordinary actor (player or monster) is blocked from moving onto (x, y) right
    /// now -- an off-map tile, a wall/other non-walkable terrain, another actor, a closed door,
    /// a chest (open or closed -- see the Persistent Containers proposal's own explicit
    /// recommendation: the physical chest remains present either way), or a movement-blocking
    /// RoomObject. Traps/ground items never block this (an actor walks onto/through those fine;
    /// triggering/picking up is a separate concern from raw passability) -- see
    /// IsBlockedForObjectPlacement for the stricter check pushing/generation need instead. The
    /// single source of truth every movement-adjacent system (GameLoop.HandleMove, ChaseAI.TryMove,
    /// TerrainPathfinder, FindFreeAdjacentTile, TeleportEffect, RayTracer) should consult instead
    /// of re-deriving its own definition of "open" (design spec: "so player movement, monster
    /// movement, teleportation, generation, and pushing do not develop conflicting definitions of
    /// an open tile").
    /// </summary>
    public bool IsBlockedForActorMovement(int x, int y) =>
        !IsWalkable(x, y) || GetActorAt(x, y) != null || GetDoorAt(x, y) != null || GetChestAt(x, y) != null
        || GetRoomObjectAt(x, y) is { BlocksMovement: true };

    /// <summary>
    /// Stricter than IsBlockedForActorMovement -- whether (x, y) is a valid destination for a
    /// PUSHED room object, or for placing a new one at generation time. Everything the movement
    /// check already rejects, PLUS: any room object at all (even a non-blocking one -- two
    /// objects can't share a tile), the stairs, a chest, a ground item, or a trap. None of those
    /// stop an ACTOR from walking there, but a boulder landing on top of any of them would be
    /// ambiguous (design spec: "onto another feature whose behavior would be ambiguous, such as
    /// an item or active trap").
    /// </summary>
    public bool IsBlockedForObjectPlacement(int x, int y) =>
        !IsWalkable(x, y) || GetActorAt(x, y) != null || GetDoorAt(x, y) != null || GetRoomObjectAt(x, y) != null
        || (x, y) == StairsUpPosition || (x, y) == StairsDownPosition
        || GetChestAt(x, y) != null || GetGroundItemsAt(x, y).Count > 0 || GetTrapAt(x, y) != null;

    private static readonly (int Dx, int Dy)[] AdjacentOffsets =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0), (-1, -1), (1, -1), (-1, 1), (1, 1)
    };

    /// <summary>The first walkable, unoccupied tile next to (x,y) -- used by Shadowstep, which teleports beside a target rather than to a specific ("behind") side, since no facing concept exists in this game.</summary>
    public (int X, int Y)? FindFreeAdjacentTile(int x, int y)
    {
        foreach (var (dx, dy) in AdjacentOffsets)
        {
            int nx = x + dx, ny = y + dy;
            if (!IsBlockedForActorMovement(nx, ny))
            {
                return (nx, ny);
            }
        }
        return null;
    }

    /// <summary>
    /// Flood-fills every tile physically reachable from StairsUpPosition (8-directional, matching
    /// the numpad's diagonal movement) across the level's base walkable terrain -- ignoring doors/
    /// chests/room-objects/actors entirely, since a closed or even locked door still counts as part
    /// of the graph (it can eventually be opened/bashed/keyed), while those other obstacles are
    /// transient occupancy, not a permanent map-graph disconnection. Every reached floor tile, plus
    /// every wall tile touching one, is marked IsReachable; anything else (a pocket a generation
    /// bug left disconnected, or a room behind a still-undiscovered hidden door) stays unreachable.
    /// Called once after generation (DungeonGenerator), once after restoring a saved floor
    /// (SaveManager.FromLevelData), and again whenever a hidden door gets carved into a wall
    /// (RoomObjectTriggerProcessor's RevealHiddenDoor case) -- see Tile.IsReachable's own doc
    /// comment for why FieldOfView consults this instead of trusting raw sight-line geometry alone.
    /// </summary>
    public void RecomputeReachability()
    {
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                Tiles[x, y].IsReachable = false;
            }
        }

        var (startX, startY) = StairsUpPosition;
        if (!IsInBounds(startX, startY) || !Tiles[startX, startY].IsWalkable)
        {
            return; // Should never happen on a properly generated/restored floor -- defensive only.
        }

        var visited = new bool[Width, Height];
        var queue = new Queue<(int X, int Y)>();
        visited[startX, startY] = true;
        queue.Enqueue((startX, startY));

        while (queue.Count > 0)
        {
            var (cx, cy) = queue.Dequeue();
            Tiles[cx, cy].IsReachable = true;

            foreach (var (dx, dy) in AdjacentOffsets)
            {
                int nx = cx + dx, ny = cy + dy;
                if (!IsInBounds(nx, ny) || visited[nx, ny] || !Tiles[nx, ny].IsWalkable)
                {
                    continue;
                }
                visited[nx, ny] = true;
                queue.Enqueue((nx, ny));
            }
        }

        // A wall touching a reachable FLOOR tile is reachable too, so a legitimately reachable
        // room's own bordering walls still render normally -- only a wall bordering NOTHING
        // reachable (part of a sealed-off pocket) stays hidden. Checked against `visited` (the
        // BFS's own reachable-floor set) rather than the live Tiles[].IsReachable flag being
        // written below -- otherwise a wall marked reachable earlier in this same left-to-right,
        // top-to-bottom scan could make its NEXT-door wall look reachable too, letting
        // reachability leak arbitrarily far through a solid, multi-tile-thick mass of pure wall
        // one tile at a time purely by iteration order, instead of stopping exactly one tile
        // outside real floor everywhere.
        for (int x = 0; x < Width; x++)
        {
            for (int y = 0; y < Height; y++)
            {
                if (Tiles[x, y].IsWalkable)
                {
                    continue;
                }
                foreach (var (dx, dy) in AdjacentOffsets)
                {
                    int nx = x + dx, ny = y + dy;
                    if (IsInBounds(nx, ny) && visited[nx, ny])
                    {
                        Tiles[x, y].IsReachable = true;
                        break;
                    }
                }
            }
        }
    }

    public Actor GetActorAt(int x, int y)
    {
        foreach (var actor in Actors)
        {
            if (actor.IsAlive && actor.X == x && actor.Y == y)
            {
                return actor;
            }
        }
        return null;
    }

    /// <summary>The first VISIBLE item on this tile, if any -- concealed items (see GroundItem.IsConcealed) never come back from this on purpose, so the map glyph never leaks a misplaced item's presence. See GetItemsAt for the full visible list, GetGroundItemsAt for the unfiltered one discovery code needs.</summary>
    public Item GetItemAt(int x, int y)
    {
        foreach (var drop in GroundItems)
        {
            if (drop.X == x && drop.Y == y && !drop.IsConcealed)
            {
                return drop.Item;
            }
        }
        return null;
    }

    /// <summary>Every VISIBLE item on this tile -- a tile can hold any number at once (monster loot, player drops, floor spawns can all stack). Concealed items are excluded, same reasoning as GetItemAt. See GameLoop.HandlePickUp/HandleDropItem and Renderer's floor-item line.</summary>
    public List<Item> GetItemsAt(int x, int y) => GroundItems.Where(drop => drop.X == x && drop.Y == y && !drop.IsConcealed).Select(drop => drop.Item).ToList();

    /// <summary>Every GroundItem on this tile regardless of concealment -- for Active Search, passive discovery, and persistence, none of which should be blind to what GetItemAt/GetItemsAt deliberately hide.</summary>
    public List<GroundItem> GetGroundItemsAt(int x, int y) => GroundItems.Where(drop => drop.X == x && drop.Y == y).ToList();

    /// <summary>Places an always-visible item -- the vast majority of ground items (dungeon loot, monster drops, and any drop/throw that didn't roll Misplaced) go through this. Kept under the original "AddItem" name too (as a Generated-origin alias) so the many pre-existing call sites that only ever place ordinary visible items don't need to change.</summary>
    public GroundItem AddVisibleItem(int x, int y, Item item, ItemLandingOrigin origin)
    {
        var groundItem = new GroundItem(x, y, item) { LandingOrigin = origin };
        GroundItems.Add(groundItem);
        return groundItem;
    }

    public void AddItem(int x, int y, Item item) => AddVisibleItem(x, y, item, ItemLandingOrigin.Generated);

    /// <summary>Places a concealed (Misplaced) item -- excluded from GetItemAt/GetItemsAt until Active Search or passive discovery reveals it. See ItemLandingResolver, the only caller.</summary>
    public GroundItem AddConcealedItem(int x, int y, Item item, int concealmentDifficulty, ItemLandingOrigin origin)
    {
        var groundItem = new GroundItem(x, y, item)
        {
            IsConcealed = true,
            ConcealmentDifficulty = concealmentDifficulty,
            LandingOrigin = origin
        };
        GroundItems.Add(groundItem);
        return groundItem;
    }

    /// <summary>Unlike GetDoorAt/GetTrapAt, this does NOT exclude an already-open chest -- a Chest is a persistent container that stays interactable (reopen/close, put/remove items) after opening, not a one-shot object that becomes invisible once used.</summary>
    public Chest GetChestAt(int x, int y) => Chests.FirstOrDefault(c => c.X == x && c.Y == y);

    /// <summary>Every loot-bearing corpse at (x, y) -- multiple corpses may share one tile (proposal section 9). Unfiltered by visibility; callers that need visibility gating (Renderer, HandleLookHere) apply it themselves, same convention as GetChestAt/GetTrapAt.</summary>
    public List<Corpse> GetCorpsesAt(int x, int y) => Corpses.Where(c => c.X == x && c.Y == y).ToList();

    public Trap GetTrapAt(int x, int y) => Traps.FirstOrDefault(t => t.X == x && t.Y == y && !t.IsTriggered);

    /// <summary>Returns null once opened -- both HandleMove and ChaseAI.TryMove then treat the tile as plain floor.</summary>
    public Door GetDoorAt(int x, int y) => Doors.FirstOrDefault(d => d.X == x && d.Y == y && !d.IsOpen);

    /// <summary>Removes exactly this item instance -- not "whatever's at x,y" -- so removing one item never disturbs any others sharing the same tile.</summary>
    public void RemoveItem(Item item) => GroundItems.RemoveAll(drop => ReferenceEquals(drop.Item, item));

    /// <summary>
    /// Drops dead non-player actors from the level so they stop blocking
    /// tiles and rendering. The player's corpse is left in place --
    /// GameLoop handles death/permadeath explicitly.
    /// </summary>
    public void RemoveDeadActors()
    {
        Actors.RemoveAll(a => !a.IsAlive && a is not Player);
    }
}
