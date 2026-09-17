using BENEATH_FORGOTTEN_STONE.Core;
using BENEATH_FORGOTTEN_STONE.Entities;
using BENEATH_FORGOTTEN_STONE.Entities.Components;
using BENEATH_FORGOTTEN_STONE.Entities.Sounds;

namespace BENEATH_FORGOTTEN_STONE.Dungeon;

/// <summary>
/// Random-rooms-and-halls generator. Every walkable tile belongs to exactly one of two
/// shapes: a "room" (any WxH rectangle) or a "hall" (a path exactly 1 tile wide for its
/// entire length, walls on both flanks everywhere except the door tile at each end) -- see
/// IsHallSegmentClear/IsAreaClear, which validate a candidate room/hall against every tile
/// already carved (not just other Room rectangles) before anything is committed, so one
/// connection can never graze, run parallel to, or cut through another room or hall it has
/// nothing to do with. Rooms are placed until a room budget is hit, skipping any that
/// overlap already-carved ground; consecutive rooms are then connected with an L-shaped
/// hall (one random bend) so the whole floor is guaranteed reachable from the entrance.
/// </summary>
public static class DungeonGenerator
{
    private class Room
    {
        public int X;
        public int Y;
        public int Width;
        public int Height;

        /// <summary>Generation metadata only -- see AssignSpecialFloors. Gameplay must always read Tile.FloorType instead; this is never consulted outside this generator (and Diagnostics/SelfTest.cs, via the test-only Generate overload below).</summary>
        public bool IsSpecialFloorRoom;
        public FloorType SpecialFloorType = FloorType.Normal;
        public double SpecialFloorCoverage = 1.0;

        /// <summary>
        /// Generation metadata only -- rolled by RollDarkRooms BEFORE doors are resolved (unlike
        /// IsSpecialFloorRoom above, which is decided after) so SpawnDoor can force a door at
        /// every one of this room's entrances once it's known to be dark -- see SpawnDoor's
        /// forceDoor parameter. Gameplay must always read Tile.IsDarkRoom instead (see
        /// PaintDarkRoomTiles, which derives it from this).
        /// </summary>
        public bool IsDarkRoom;

        public int CenterX => X + (Width / 2);
        public int CenterY => Y + (Height / 2);

        public bool Intersects(Room other) =>
            X <= other.X + other.Width && X + Width >= other.X &&
            Y <= other.Y + other.Height && Y + Height >= other.Y;
    }

    /// <summary>A door candidate recorded during carving, resolved and validated only after the whole floor is fully carved -- see the comment above the resolution loop in Generate.</summary>
    private readonly record struct DoorCandidate(Room Room, int FixedCoord, int TowardCoord, bool CorridorIsVertical);

    public static Level Generate(int floorIndex, int width, int height, Random rng, int difficultyLevel, bool forceTraderSpawn = false) =>
        Generate(floorIndex, width, height, rng, difficultyLevel, forceTraderSpawn, out _);

    /// <summary>
    /// Test-only overload exposing the generated room rectangles (including the boss room's,
    /// if one was created) so SelfTest can distinguish "inside a deliberately-placed room"
    /// from "hall" without duplicating this generator's own placement logic. The public
    /// overload above (used by DungeonManager) just discards this.
    /// </summary>
    internal static Level Generate(int floorIndex, int width, int height, Random rng, int difficultyLevel, bool forceTraderSpawn,
        out IReadOnlyList<(int X, int Y, int Width, int Height)> roomRects) =>
        Generate(floorIndex, width, height, rng, difficultyLevel, forceTraderSpawn, out roomRects, out _);

    /// <summary>
    /// Test-only overload additionally exposing each room's special-floor generation metadata
    /// (see Room.IsSpecialFloorRoom/SpecialFloorType/SpecialFloorCoverage) -- Diagnostics/
    /// SelfTest.cs uses this to validate the floor-type room rules without duplicating this
    /// generator's own room-selection logic. The boss room never appears here (see
    /// TrySpawnBossRoom's own doc comment -- it's deliberately excluded from every generic
    /// per-room pass, floor-type assignment included).
    /// </summary>
    internal static Level Generate(int floorIndex, int width, int height, Random rng, int difficultyLevel, bool forceTraderSpawn,
        out IReadOnlyList<(int X, int Y, int Width, int Height)> roomRects,
        out IReadOnlyList<(int X, int Y, int Width, int Height, bool IsSpecialFloorRoom, FloorType SpecialFloorType, double SpecialFloorCoverage)> roomFloorInfo)
    {
        var level = new Level(floorIndex, width, height);
        var rooms = new List<Room>();
        var doorCandidates = new List<DoorCandidate>();

        const int maxRooms = 14;
        const int minSize = 4;
        const int maxSize = 9;
        const int maxAttempts = 20;

        for (int i = 0; i < maxRooms; i++)
        {
            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                int w = rng.Next(minSize, maxSize + 1);
                int h = rng.Next(minSize, maxSize + 1);
                int x = rng.Next(1, Math.Max(2, width - w - 1));
                int y = rng.Next(1, Math.Max(2, height - h - 1));

                var candidate = new Room { X = x, Y = y, Width = w, Height = h };

                // IsAreaClear (not just Intersects against other Room rectangles) confirms the
                // candidate's whole footprint is still virgin wall -- Intersects alone can't see
                // an earlier connection's hall, which isn't a Room and could otherwise be cut
                // straight through by this new room.
                if (rooms.Any(r => candidate.Intersects(r)) || !IsAreaClear(level, candidate))
                {
                    continue;
                }

                if (rooms.Count == 0)
                {
                    CarveRoom(level, candidate);
                    rooms.Add(candidate);
                    break;
                }

                var previous = rooms[^1];
                bool horizontalFirst = rng.Next(2) == 0;

                // Random point along each room's own interior span, not always dead-center --
                // matches TrySpawnBossRoom's own approach, and avoids every connection through
                // a given room reusing the exact same row/column as every other one.
                int fixedCoordPrevious, fixedCoordCandidate;
                bool leg1Vertical, leg2Vertical;
                if (horizontalFirst)
                {
                    // Leg 1 exits `previous` horizontally; leg 2 enters `candidate` vertically.
                    fixedCoordPrevious = rng.Next(previous.Y + 1, previous.Y + previous.Height - 1);
                    fixedCoordCandidate = rng.Next(candidate.X + 1, candidate.X + candidate.Width - 1);
                    leg1Vertical = false;
                    leg2Vertical = true;
                }
                else
                {
                    // Leg 1 exits `previous` vertically; leg 2 enters `candidate` horizontally.
                    fixedCoordPrevious = rng.Next(previous.X + 1, previous.X + previous.Width - 1);
                    fixedCoordCandidate = rng.Next(candidate.Y + 1, candidate.Y + candidate.Height - 1);
                    leg1Vertical = true;
                    leg2Vertical = false;
                }

                var exitFromPrevious = FindRoomEntrance(previous, fixedCoordPrevious, fixedCoordCandidate, leg1Vertical);
                var entryToCandidate = FindRoomEntrance(candidate, fixedCoordCandidate, fixedCoordPrevious, leg2Vertical);
                if (exitFromPrevious == null || entryToCandidate == null)
                {
                    continue;
                }

                int leg1From = leg1Vertical ? exitFromPrevious.Value.Y : exitFromPrevious.Value.X;
                int leg2To = leg2Vertical ? entryToCandidate.Value.Y : entryToCandidate.Value.X;

                // Nothing carved yet for this attempt -- confirm both legs (their full length,
                // not just their door ends) are still solid wall before touching any tiles, so
                // a failed attempt costs nothing and just rerolls a fresh candidate.
                if (!IsHallSegmentClear(level, fixedCoordPrevious, leg1From, fixedCoordCandidate, leg1Vertical, candidate)
                    || !IsHallSegmentClear(level, fixedCoordCandidate, fixedCoordPrevious, leg2To, leg2Vertical, candidate))
                {
                    continue;
                }

                CarveRoom(level, candidate);
                if (leg1Vertical)
                {
                    CarveVerticalCorridor(level, leg1From, fixedCoordCandidate, fixedCoordPrevious);
                }
                else
                {
                    CarveHorizontalCorridor(level, leg1From, fixedCoordCandidate, fixedCoordPrevious);
                }
                if (leg2Vertical)
                {
                    CarveVerticalCorridor(level, fixedCoordPrevious, leg2To, fixedCoordCandidate);
                }
                else
                {
                    CarveHorizontalCorridor(level, fixedCoordPrevious, leg2To, fixedCoordCandidate);
                }

                doorCandidates.Add(new DoorCandidate(previous, fixedCoordPrevious, fixedCoordCandidate, leg1Vertical));
                doorCandidates.Add(new DoorCandidate(candidate, fixedCoordCandidate, fixedCoordPrevious, leg2Vertical));

                rooms.Add(candidate);
                break;
            }
        }

        if (rooms.Count == 0)
        {
            // Guarantee the floor is never unplayable even if random placement kept failing.
            var fallback = new Room { X = 2, Y = 2, Width = 6, Height = 6 };
            CarveRoom(level, fallback);
            rooms.Add(fallback);
        }

        var firstRoom = rooms[0];
        var lastRoom = rooms[^1];

        // Rolled BEFORE doors are resolved below (unlike AssignSpecialFloors' own room roll,
        // which happens after) -- see SpawnDoor's forceDoor parameter and RollDarkRooms' own
        // doc comment on why the ordering matters here specifically.
        RollDarkRooms(level, rooms, firstRoom, lastRoom, rng, difficultyLevel);

        // Floor 1 (the ground floor) has nowhere "up" to go, so leave its entrance as plain floor.
        if (floorIndex > 1)
        {
            level.Tiles[firstRoom.CenterX, firstRoom.CenterY] = Tile.CreateStairsUp();
        }
        level.StairsUpPosition = (firstRoom.CenterX, firstRoom.CenterY);

        level.Tiles[lastRoom.CenterX, lastRoom.CenterY] = Tile.CreateStairsDown();
        level.StairsDownPosition = (lastRoom.CenterX, lastRoom.CenterY);

        // Doors are only resolved now, against the FULLY carved floor -- resolving them
        // inline during the room loop above checked each candidate's perpendicular walls
        // before every room existed yet, so a room added later could carve a corridor right
        // alongside an already-placed door and silently open up a way around it. Waiting
        // until every room and corridor is carved means the chokepoint check reflects the
        // floor's real, final shape.
        foreach (var candidate in doorCandidates)
        {
            TrySpawnDoorAtEntrance(level, candidate.Room, candidate.FixedCoord, candidate.TowardCoord, candidate.CorridorIsVertical, rng, difficultyLevel);
        }

        // After doors (so a room's true walkable-floor footprint, excluding the stairs tiles
        // already placed above, is fully known) and before any content spawns -- floor type
        // has no bearing on monster/item/chest/trap placement, so order relative to those
        // passes doesn't matter functionally, but doing it here matches the design spec's own
        // generation order most closely.
        AssignSpecialFloors(level, rooms, rng);

        // The actual Tile.IsDarkRoom painting (interior only -- see PaintDarkRoomTiles' own doc
        // comment on why walls are deliberately excluded) -- separate from RollDarkRooms above,
        // and fine to do only now since it has no bearing on door placement (already resolved by
        // this point using each Room's own IsDarkRoom flag).
        PaintDarkRoomTiles(level, rooms);

        SpawnMonsters(level, rooms, rng, difficultyLevel);
        SpawnItems(level, rooms, rng);
        SpawnChests(level, rooms, rng, difficultyLevel);
        SpawnTraps(level, rooms, rng, difficultyLevel);
        SpawnRoomObjects(level, rooms, rng);
        SpawnSkeletonKeys(level, rooms, rng);
        SpawnTrader(level, rooms, rng, difficultyLevel, forceTraderSpawn);

        // Deliberately last, and deliberately never added to `rooms` -- everything above
        // already ran against the main chain, so a boss room (created fresh right here) can
        // never have extra monsters/items/chests/traps scattered into it by those generic
        // passes. See TrySpawnBossRoom's own doc comment for why this needs a brand new
        // room instead of picking one from the main chain.
        var bossRoomRect = TrySpawnBossRoom(level, rooms, rng, difficultyLevel);

        var rects = rooms.Select(r => (r.X, r.Y, r.Width, r.Height)).ToList();
        if (bossRoomRect.HasValue)
        {
            rects.Add(bossRoomRect.Value);
        }
        roomRects = rects;

        // The boss room is deliberately excluded here too -- see this method's own doc comment.
        roomFloorInfo = rooms
            .Select(r => (r.X, r.Y, r.Width, r.Height, r.IsSpecialFloorRoom, r.SpecialFloorType, r.SpecialFloorCoverage))
            .ToList();

        level.RecomputeReachability();

        return level;
    }

    /// <summary>
    /// Picks a random subset of `rooms` (never the boss room -- it isn't in this list, see
    /// TrySpawnBossRoom's own doc comment) to receive one special FloorType each, generated as
    /// a clustered region rather than scattered individual tiles. A level may end up with zero
    /// affected rooms (MinimumAffectedRooms defaults to 0) -- there's no requirement that every
    /// floor have one. Each selected room independently rolls its own FloorType and coverage,
    /// so the same type can appear in several rooms, or several different types can appear
    /// across one level, with no relationship between them.
    /// </summary>
    private static void AssignSpecialFloors(Level level, List<Room> rooms, Random rng)
    {
        if (rooms.Count == 0)
        {
            return;
        }

        int maxAffected = Math.Min(rooms.Count, FloorTypeConfig.MaximumAffectedRooms);
        if (maxAffected <= FloorTypeConfig.MinimumAffectedRooms)
        {
            maxAffected = FloorTypeConfig.MinimumAffectedRooms;
        }
        int affectedCount = rng.Next(FloorTypeConfig.MinimumAffectedRooms, maxAffected + 1);
        if (affectedCount == 0)
        {
            return;
        }

        var candidates = new List<Room>(rooms);
        for (int i = 0; i < affectedCount && candidates.Count > 0; i++)
        {
            int index = rng.Next(candidates.Count);
            var room = candidates[index];
            candidates.RemoveAt(index);

            var floorType = FloorTypeCatalog.SpecialTypes[rng.Next(FloorTypeCatalog.SpecialTypes.Count)];
            bool created = TryGenerateSpecialFloorRegion(level, room, floorType, rng);
            // No fallback action needed on failure -- TryGenerateSpecialFloorRegion leaves the
            // room's tiles as whatever they already were (plain Normal-default Floor) and Room
            // itself defaults to IsSpecialFloorRoom = false, satisfying "reset to NORMAL" as a
            // simple do-nothing rather than an explicit revert step.

            // Ambient Sound / Hearing System spec section 3: not every special-floor room
            // audibly announces itself -- a per-FloorType chance (0 for FloorTypes with no
            // EnvironmentSoundCatalog entry, e.g. Grass/Sand/Mud/Ash) decides whether this one
            // does. Source position is the room's own center, one fixed point per room.
            if (created && rng.NextDouble() < SoundConfig.EnvironmentSoundSourceChance(floorType))
            {
                level.AmbientSoundSources.Add(new AmbientSoundSource(room.CenterX, room.CenterY, floorType));
            }
        }
    }

    /// <summary>
    /// Independently rolls each eligible room (every room except the stairs-up/stairs-down ones)
    /// for LightingConfig.DarkRoomChance, setting Room.IsDarkRoom (generation metadata only --
    /// see PaintDarkRoomTiles for the actual Tile.IsDarkRoom that gameplay reads) and directly
    /// placing a forced door at every genuine entrance found by TryFindEveryEntranceForDarkRoom
    /// -- deliberately NOT limited to this room's own doorCandidates entries (its two FORMAL
    /// connections), because a corridor belonging to an entirely UNRELATED connection can
    /// legitimately run adjacent to this room's wall for a stretch on its way to wherever it
    /// actually enters (corridors are only checked against already-carved tiles for overlap,
    /// never for mere adjacency to a room they don't connect to) -- that's just as real a
    /// line-of-sight leak into a dark room as the formal entrance is, and doorCandidates alone
    /// would silently miss it. A room whose geometry can't support a genuine door at every one of
    /// its discovered entrances (see IsValidDoorChokepoint) is left un-darkened entirely rather
    /// than darkened with a leaky one -- there's no requirement that a fixed number of rooms end
    /// up dark, so skipping an occasional ineligible one is a non-issue. Runs BEFORE the normal
    /// door-resolution loop in Generate; that loop's own SpawnDoor calls become no-ops wherever
    /// this already placed one (see its GetDoorAt guard), so running first is safe either way. A
    /// room may end up dark regardless of its FloorType (a dark Water room is a perfectly valid
    /// combination; nothing here checks AssignSpecialFloors' own choices).
    /// </summary>
    private static void RollDarkRooms(Level level, List<Room> rooms, Room firstRoom, Room lastRoom, Random rng, int difficultyLevel)
    {
        foreach (var room in rooms)
        {
            if (room == firstRoom || room == lastRoom || rng.NextDouble() >= LightingConfig.DarkRoomChance)
            {
                continue;
            }

            if (!TryFindEveryEntranceForDarkRoom(level, room, out var entrances))
            {
                continue;
            }

            room.IsDarkRoom = true;
            foreach (var (x, y, corridorIsVertical) in entrances)
            {
                SpawnDoor(level, x, y, corridorIsVertical, rng, difficultyLevel, forceDoor: true);
            }
        }
    }

    /// <summary>
    /// Scans the full ring of tiles immediately surrounding `room` for every genuine entrance --
    /// any FLOOR tile bordering the room's interior along one of its four edges (corners
    /// excluded -- a diagonal tile touches no interior tile directly). This deliberately doesn't
    /// care whether a given entrance is one of `room`'s own formal doorCandidates or just an
    /// unrelated corridor leg passing adjacent to it -- see RollDarkRooms' own doc comment on why
    /// both need a door equally. Returns false the moment any discovered entrance fails
    /// IsValidDoorChokepoint (with `entrances` left incomplete -- the caller should discard it),
    /// and false if none are found at all (shouldn't happen for any room actually reachable in a
    /// connected dungeon, but a room with zero entrances certainly can't be safely darkened).
    /// </summary>
    private static bool TryFindEveryEntranceForDarkRoom(Level level, Room room, out List<(int X, int Y, bool CorridorIsVertical)> entrances)
    {
        entrances = new List<(int X, int Y, bool CorridorIsVertical)>();

        for (int x = room.X - 1; x <= room.X + room.Width; x++)
        {
            for (int y = room.Y - 1; y <= room.Y + room.Height; y++)
            {
                if (!level.IsInBounds(x, y))
                {
                    continue;
                }

                bool insideRoom = x >= room.X && x < room.X + room.Width && y >= room.Y && y < room.Y + room.Height;
                bool onLeftOrRightEdge = x == room.X - 1 || x == room.X + room.Width;
                bool onTopOrBottomEdge = y == room.Y - 1 || y == room.Y + room.Height;
                bool isCorner = onLeftOrRightEdge && onTopOrBottomEdge;

                if (insideRoom || isCorner || (!onLeftOrRightEdge && !onTopOrBottomEdge) || level.Tiles[x, y].Type != TileType.Floor)
                {
                    continue;
                }

                // A top/bottom-edge opening leads to a corridor traveling vertically (matches
                // FindRoomEntrance's own convention); a left/right-edge opening, horizontally.
                bool corridorIsVertical = onTopOrBottomEdge;
                if (!IsValidDoorChokepoint(level, x, y, corridorIsVertical))
                {
                    return false;
                }

                entrances.Add((x, y, corridorIsVertical));
            }
        }

        return entrances.Count > 0;
    }

    /// <summary>
    /// Marks every INTERIOR tile of a Room already flagged IsDarkRoom (see RollDarkRooms) --
    /// deliberately NOT the wall ring around it. Walls are opaque boundary structures visible
    /// from either side regardless of what's beyond them (exactly like a door -- see
    /// RollDarkRooms' own doc comment on why door tiles are never marked dark either); what
    /// actually keeps a dark room's own walls hidden from someone standing blind inside it isn't
    /// this flag at all, it's FieldOfView.Compute's observer-blindness rule, which withholds
    /// anything not currently illuminated or already explored regardless of the tile's own
    /// IsDarkRoom state. An earlier version of this method DID mark the wall ring too (needed
    /// under FieldOfView's older, tile-only gating, before the observer-blindness rework) -- that
    /// stopped being necessary once blindness moved to the observer, and became actively wrong:
    /// a wall shared between a dark room and an ordinary, lit corridor would vanish for a player
    /// just walking down that corridor, since the wall tile itself required illumination even
    /// though the OBSERVER wasn't blind at all.
    /// </summary>
    private static void PaintDarkRoomTiles(Level level, List<Room> rooms)
    {
        foreach (var room in rooms)
        {
            if (!room.IsDarkRoom)
            {
                continue;
            }

            for (int x = room.X; x < room.X + room.Width; x++)
            {
                for (int y = room.Y; y < room.Y + room.Height; y++)
                {
                    if (level.IsInBounds(x, y))
                    {
                        level.Tiles[x, y].IsDarkRoom = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Grows one clustered region of `floorType` inside `room`'s own rectangle via randomized
    /// flood fill, covering a randomly chosen percentage of the room's walkable floor tiles
    /// between FloorTypeConfig's Minimum/MaximumSpecialFloorCoverage. Restricting every
    /// candidate tile to `room`'s own [X, X+Width) x [Y, Y+Height) rectangle -- and further to
    /// tiles that are still plain TileType.Floor (excluding the stairs tiles already placed
    /// before this runs) -- gets walls/doors/hallways/other-rooms as hard boundaries entirely
    /// for free: this generator already carves every room as a solid rectangle with every door
    /// and corridor tile OUTSIDE of it (see FindRoomEntrance), so nothing outside the rectangle
    /// is ever a candidate in the first place. No separate wall/door detection is needed.
    /// </summary>
    /// <returns>False (room left untouched, stays Normal) if the room has no walkable floor tiles to work with at all -- never fails purely for being "too small," since even a handful of tiles can satisfy a 25% minimum.</returns>
    private static bool TryGenerateSpecialFloorRegion(Level level, Room room, FloorType floorType, Random rng)
    {
        var walkable = new List<(int X, int Y)>();
        for (int x = room.X; x < room.X + room.Width; x++)
        {
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                if (level.IsInBounds(x, y) && level.Tiles[x, y].Type == TileType.Floor)
                {
                    walkable.Add((x, y));
                }
            }
        }

        if (walkable.Count == 0)
        {
            return false;
        }

        double coveragePercent = FloorTypeConfig.MinimumSpecialFloorCoverage
            + (rng.NextDouble() * (FloorTypeConfig.MaximumSpecialFloorCoverage - FloorTypeConfig.MinimumSpecialFloorCoverage));
        int minimumTiles = (int)Math.Ceiling(walkable.Count * FloorTypeConfig.MinimumSpecialFloorCoverage);
        int targetTiles = Math.Clamp((int)Math.Ceiling(walkable.Count * coveragePercent), minimumTiles, walkable.Count);

        var walkableSet = new HashSet<(int X, int Y)>(walkable);
        var affected = new HashSet<(int X, int Y)>();

        // Reserve a small slice of the budget for an optional secondary cluster, grown after
        // the main region -- see GrowRegion's own comment on why growth can plateau early.
        int secondaryBudget = rng.NextDouble() < FloorTypeConfig.SecondaryClusterChance
            ? Math.Min(targetTiles / 4 + 1, walkable.Count - 1)
            : 0;
        int primaryTarget = targetTiles - secondaryBudget;

        var seed = walkable[rng.Next(walkable.Count)];
        GrowRegion(walkableSet, affected, seed, primaryTarget, rng);

        if (secondaryBudget > 0)
        {
            var remaining = walkable.Where(t => !affected.Contains(t)).ToList();
            if (remaining.Count > 0)
            {
                var secondarySeed = remaining[rng.Next(remaining.Count)];
                GrowRegion(walkableSet, affected, secondarySeed, affected.Count + secondaryBudget, rng);
            }
        }

        double finalCoverage = affected.Count / (double)walkable.Count;
        if (finalCoverage < FloorTypeConfig.MinimumSpecialFloorCoverage)
        {
            // Couldn't reach the minimum (e.g. an oddly-shaped or very small room) -- leave it
            // Normal entirely rather than produce a room that violates the coverage rule.
            return false;
        }

        foreach (var (x, y) in affected)
        {
            level.Tiles[x, y].FloorType = floorType;
        }

        room.IsSpecialFloorRoom = true;
        room.SpecialFloorType = floorType;
        room.SpecialFloorCoverage = finalCoverage;
        return true;
    }

    /// <summary>
    /// Randomized flood fill: repeatedly pulls a random tile from the current frontier (every
    /// not-yet-affected walkable tile adjacent to the affected set) and adds it, rather than
    /// picking uniformly at random from the whole room -- this is what produces one clustered
    /// blob instead of scattered individual tiles. Stops early (before reaching `targetCount`)
    /// if the frontier runs dry, which can happen in a small or oddly-shaped room; the caller
    /// checks final coverage against the minimum afterward rather than this method guaranteeing
    /// it reached the target exactly.
    /// </summary>
    private static void GrowRegion(HashSet<(int X, int Y)> walkableSet, HashSet<(int X, int Y)> affected, (int X, int Y) seed, int targetCount, Random rng)
    {
        if (!walkableSet.Contains(seed))
        {
            return;
        }

        affected.Add(seed);
        var frontier = new List<(int X, int Y)>();
        AddNeighborsToFrontier(walkableSet, affected, frontier, seed);

        while (affected.Count < targetCount && frontier.Count > 0)
        {
            int index = rng.Next(frontier.Count);
            var next = frontier[index];
            frontier.RemoveAt(index);

            if (affected.Contains(next))
            {
                continue;
            }

            affected.Add(next);
            AddNeighborsToFrontier(walkableSet, affected, frontier, next);
        }
    }

    private static readonly (int Dx, int Dy)[] FloorGrowthOffsets = { (0, -1), (0, 1), (-1, 0), (1, 0) };

    private static void AddNeighborsToFrontier(HashSet<(int X, int Y)> walkableSet, HashSet<(int X, int Y)> affected, List<(int X, int Y)> frontier, (int X, int Y) tile)
    {
        foreach (var (dx, dy) in FloorGrowthOffsets)
        {
            var neighbor = (tile.X + dx, tile.Y + dy);
            if (walkableSet.Contains(neighbor) && !affected.Contains(neighbor))
            {
                frontier.Add(neighbor);
            }
        }
    }

    /// <summary>
    /// A boss room needs exactly one entrance and must never contain stairs (see
    /// "Boss Monster and Boss Room System.txt"). Under this generator's own topology --
    /// a single linear chain of rooms, each connected to the next (see the type-level
    /// comment above and SpawnDoor's) -- no room other than the stairs rooms themselves
    /// ever has exactly one entrance, and those are exactly the ones the boss room must
    /// exclude. So rather than retrofitting entrance-counting onto the main chain, this
    /// carves a brand new, purpose-built dead-end room branching off a random point on
    /// the existing floor via one short corridor -- it has exactly one entrance by
    /// construction, since nothing else is ever connected to it.
    /// </summary>
    private static (int X, int Y, int Width, int Height)? TrySpawnBossRoom(Level level, List<Room> rooms, Random rng, int difficultyLevel)
    {
        if (!BossConfig.BossSpawnEnabled || rng.NextDouble() > BossConfig.BossRoomSpawnChance)
        {
            return null;
        }

        // Larger floor than the main generator's minSize=4 -- PopulateBossRoom needs to fit
        // the boss plus up to BossMaxEntourage minions (9 actors worst case) inside the
        // room's interior. A 4x4 room's 2x2 interior (4 tiles) can't hold that many, silently
        // dropping entourage members whenever their random tile pick collides with one
        // already taken. minSize=6 gives a 4x4 interior (16 tiles) at the smallest, always
        // comfortably more than enough.
        const int minSize = 6;
        const int maxSize = 9;
        const int maxAttempts = 20;

        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            var host = rooms[rng.Next(rooms.Count)];
            // A dark room is never eligible as a host: this spur's own door only ever guards the
            // BOSS room's end (line ~707 below), leaving the host's own boundary wide open --
            // exactly the undoored line-of-sight leak dark rooms can never have (see
            // RollDarkRooms). Simplest fix is exclusion, matching how stairs rooms are already
            // excluded from BEING dark in the first place, rather than teaching this entirely
            // separate spur-carving system to also force a door at the host's boundary.
            if (host.Width <= 2 || host.Height <= 2 || host.IsDarkRoom)
            {
                continue;
            }

            int direction = rng.Next(4); // 0 = North, 1 = East, 2 = South, 3 = West
            int corridorLength = rng.Next(2, 5);
            int w = rng.Next(minSize, maxSize + 1);
            int h = rng.Next(minSize, maxSize + 1);

            Room candidate;
            bool corridorIsVertical;
            int fixedCoord, towardCoord, hostBoundary;

            switch (direction)
            {
                case 0: // North -- candidate sits above host
                    int northX = rng.Next(host.X + 1, host.X + host.Width - 1);
                    candidate = new Room { X = northX - (w / 2), Y = host.Y - corridorLength - h, Width = w, Height = h };
                    corridorIsVertical = true;
                    fixedCoord = northX;
                    towardCoord = host.CenterY;
                    hostBoundary = host.Y - 1;
                    break;

                case 1: // East -- candidate sits right of host
                    int eastY = rng.Next(host.Y + 1, host.Y + host.Height - 1);
                    candidate = new Room { X = host.X + host.Width + corridorLength, Y = eastY - (h / 2), Width = w, Height = h };
                    corridorIsVertical = false;
                    fixedCoord = eastY;
                    towardCoord = host.CenterX;
                    hostBoundary = host.X + host.Width;
                    break;

                case 2: // South -- candidate sits below host
                    int southX = rng.Next(host.X + 1, host.X + host.Width - 1);
                    candidate = new Room { X = southX - (w / 2), Y = host.Y + host.Height + corridorLength, Width = w, Height = h };
                    corridorIsVertical = true;
                    fixedCoord = southX;
                    towardCoord = host.CenterY;
                    hostBoundary = host.Y + host.Height;
                    break;

                default: // West -- candidate sits left of host
                    int westY = rng.Next(host.Y + 1, host.Y + host.Height - 1);
                    candidate = new Room { X = host.X - corridorLength - w, Y = westY - (h / 2), Width = w, Height = h };
                    corridorIsVertical = false;
                    fixedCoord = westY;
                    towardCoord = host.CenterX;
                    hostBoundary = host.X - 1;
                    break;
            }

            if (candidate.X < 1 || candidate.Y < 1
                || candidate.X + candidate.Width >= level.Width - 1 || candidate.Y + candidate.Height >= level.Height - 1)
            {
                continue;
            }
            if (rooms.Any(r => candidate.Intersects(r)))
            {
                continue;
            }

            var entrance = FindRoomEntrance(candidate, fixedCoord, towardCoord, corridorIsVertical);
            if (entrance == null)
            {
                continue;
            }

            // Nothing has been carved for this attempt yet -- confirm the room's own
            // footprint and the whole corridor path (its full length, not just the door end)
            // are still solid wall before touching any tiles. This is the same safeguard
            // SpawnDoor already applies for the main chain, needed here because an existing
            // hall (not a Room, so Intersects above can't see it -- and IsAreaClear/
            // IsHallSegmentClear check actual carved tiles, not just Room rectangles) could
            // otherwise run straight through this room or cross its spur and open an
            // undoored bypass into it.
            int entranceCoord = corridorIsVertical ? entrance.Value.Y : entrance.Value.X;
            if (!IsAreaClear(level, candidate)
                || !IsHallSegmentClear(level, fixedCoord, entranceCoord, hostBoundary, corridorIsVertical))
            {
                continue;
            }

            CarveRoom(level, candidate);
            if (corridorIsVertical)
            {
                CarveVerticalCorridor(level, entrance.Value.Y, hostBoundary, fixedCoord);
            }
            else
            {
                CarveHorizontalCorridor(level, entrance.Value.X, hostBoundary, fixedCoord);
            }

            bool isPickable = rng.NextDouble() < 0.75;
            int doorDifficulty = 15 + (difficultyLevel / 2);
            int doorMaxHealth = 20 + (difficultyLevel * 3);
            // Unlike SpawnDoor, this entrance always exists and is always locked -- the doc
            // requires a boss room to always be sealed, not merely sometimes. Always bashable
            // too, matching the invariant already established for every other locked door in
            // this generator (see SpawnDoor's own comment) -- a boss fight is the intended
            // obstacle here, not the door itself.
            level.Doors.Add(new Door(entrance.Value.X, entrance.Value.Y, isLocked: true, isPickable, isBashable: true, doorDifficulty, doorMaxHealth));

            // Deliberately NOT added to `rooms` -- see this method's own doc comment and the
            // comment at its call site in Generate. Every generic per-room spawn pass already
            // ran before this method is ever called, so there's nothing left that would
            // consume `rooms` and scatter extra content into the boss room anyway.
            PopulateBossRoom(level, candidate, rng, difficultyLevel);
            return (candidate.X, candidate.Y, candidate.Width, candidate.Height);
        }

        return null;
    }

    /// <summary>
    /// Every tile inside the candidate room's footprint, PLUS a 1-tile buffer ring all the
    /// way around it, must still be solid wall. The footprint check alone catches true
    /// overlap (e.g. a hall cutting straight through where this room is about to go, since a
    /// hall isn't a Room and Intersects above can't see it) -- but it does NOT catch the room
    /// ending up merely adjacent (touching, zero gap, no wall between them) to an
    /// already-carved room or hall, which silently fuses the two into one wider-than-intended
    /// area with no door anywhere along the shared edge. The buffer ring closes that gap.
    /// level.IsWalkable already returns false out of bounds, so the ring needs no clamping.
    /// </summary>
    private static bool IsAreaClear(Level level, Room room)
    {
        for (int x = room.X - 1; x <= room.X + room.Width; x++)
        {
            for (int y = room.Y - 1; y <= room.Y + room.Height; y++)
            {
                if (level.IsWalkable(x, y))
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Every tile from (fixedCoord,from) to (fixedCoord,to) along the given axis -- AND both
    /// of each tile's perpendicular neighbors -- must currently be solid wall. Checked BEFORE
    /// any carving for this attempt, so a failed candidate costs nothing. This is what
    /// guarantees a carved hall is exactly 1 tile wide for its entire length (not just at its
    /// two door ends), and that it never runs alongside an already-carved room or hall it has
    /// nothing to do with -- the root cause of the "wider than a hallway, but not a real room"
    /// areas this generator used to be able to produce.
    ///
    /// `pretendOccupied`, if given, is a room that hasn't been carved yet but is ABOUT to be
    /// (the main-chain connector validates a hall leg before committing to carve the room it
    /// leads to) -- needed because a leg's fixed row/column is chosen from the OTHER room's
    /// own span, independent of where this one landed, so it can coincidentally end up
    /// exactly one step outside pretendOccupied's edge for a stretch longer than just the
    /// single door tile. Treating it as already-solid here catches that before anything is
    /// carved, the same way it would if it had genuinely been carved first.
    /// </summary>
    private static bool IsHallSegmentClear(Level level, int fixedCoord, int from, int to, bool corridorIsVertical, Room pretendOccupied = null)
    {
        int start = Math.Min(from, to);
        int end = Math.Max(from, to);
        for (int i = start; i <= end; i++)
        {
            int x = corridorIsVertical ? fixedCoord : i;
            int y = corridorIsVertical ? i : fixedCoord;
            if (level.IsWalkable(x, y) || IsInsideRoom(pretendOccupied, x, y))
            {
                return false;
            }
            if (corridorIsVertical)
            {
                if (level.IsWalkable(x - 1, y) || IsInsideRoom(pretendOccupied, x - 1, y)
                    || level.IsWalkable(x + 1, y) || IsInsideRoom(pretendOccupied, x + 1, y))
                {
                    return false;
                }
            }
            else
            {
                if (level.IsWalkable(x, y - 1) || IsInsideRoom(pretendOccupied, x, y - 1)
                    || level.IsWalkable(x, y + 1) || IsInsideRoom(pretendOccupied, x, y + 1))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private static bool IsInsideRoom(Room room, int x, int y) =>
        room != null && x >= room.X && x < room.X + room.Width && y >= room.Y && y < room.Y + room.Height;

    /// <summary>One boss (Monster.CreateBoss) plus a random-sized normal entourage (Monster.CreateRandom, same as SpawnMonsters uses elsewhere) -- every entourage monster is a plain level-appropriate monster, never a boss itself.</summary>
    private static void PopulateBossRoom(Level level, Room room, Random rng, int difficultyLevel)
    {
        int bossX = rng.Next(room.X + 1, room.X + room.Width - 1);
        int bossY = rng.Next(room.Y + 1, room.Y + room.Height - 1);
        var boss = Monster.CreateBoss(bossX, bossY, difficultyLevel, rng);
        level.Actors.Add(boss);
        level.Scheduler.Register(boss);

        int entourageCount = rng.Next(BossConfig.BossMinEntourage, BossConfig.BossMaxEntourage + 1);
        int placed = 0;
        // Retries past a random-tile collision instead of just skipping that entourage slot --
        // the room is already sized (see TrySpawnBossRoom's minSize comment) to comfortably fit
        // every minion, so a collision should cost an attempt, not a whole minion. Bounded
        // generously so a pathological room still terminates rather than looping forever.
        int maxAttempts = entourageCount * 20;
        for (int attempt = 0; attempt < maxAttempts && placed < entourageCount; attempt++)
        {
            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);
            if (!level.IsWalkable(x, y) || level.GetActorAt(x, y) != null)
            {
                continue;
            }

            var minion = Monster.CreateRandom(x, y, difficultyLevel, rng);
            level.Actors.Add(minion);
            level.Scheduler.Register(minion);
            placed++;
        }
    }

    private static void CarveRoom(Level level, Room room)
    {
        for (int x = room.X; x < room.X + room.Width; x++)
        {
            for (int y = room.Y; y < room.Y + room.Height; y++)
            {
                if (level.IsInBounds(x, y))
                {
                    level.Tiles[x, y] = Tile.CreateFloor();
                }
            }
        }
    }

    private static void CarveHorizontalCorridor(Level level, int x1, int x2, int y)
    {
        for (int x = Math.Min(x1, x2); x <= Math.Max(x1, x2); x++)
        {
            if (level.IsInBounds(x, y))
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
    }

    private static void CarveVerticalCorridor(Level level, int y1, int y2, int x)
    {
        for (int y = Math.Min(y1, y2); y <= Math.Max(y1, y2); y++)
        {
            if (level.IsInBounds(x, y))
            {
                level.Tiles[x, y] = Tile.CreateFloor();
            }
        }
    }

    private static void SpawnMonsters(Level level, List<Room> rooms, Random rng, int difficultyLevel)
    {
        // Skip the first room (index 0) so the entrance/entry point is always safe.
        for (int i = 1; i < rooms.Count; i++)
        {
            if (rng.NextDouble() > 0.6)
            {
                continue;
            }

            var room = rooms[i];
            // See EncounterSizeCalculator's own doc comment for why this is capped rather than
            // growing unbounded with depth (e.g. floor 30: up to 16 per room x up to 9 rooms).
            int count = EncounterSizeCalculator.RollGroupSize(difficultyLevel, rng);

            for (int j = 0; j < count; j++)
            {
                if (room.Width <= 2 || room.Height <= 2)
                {
                    continue;
                }

                Monster monster = TrySpawnFloorAttunedMonster(level, room, difficultyLevel, rng);
                if (monster == null)
                {
                    int x = rng.Next(room.X + 1, room.X + room.Width - 1);
                    int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);

                    if (!level.IsWalkable(x, y) || level.GetActorAt(x, y) != null)
                    {
                        continue;
                    }

                    monster = Monster.CreateRandom(x, y, difficultyLevel, rng);
                    // Safe Monster Spawning and Hazard-Aware Movement: safety can only be checked
                    // once the actual monster exists (immunity/floor-attunement are actor-specific)
                    // -- a missing monster is preferable to one spawned somewhere it would
                    // immediately start dying, so this slot is simply skipped rather than retried,
                    // the same "skip this slot" degradation the walkability/occupancy check above
                    // already uses.
                    if (!ActorTerrainSafety.IsSafeForActor(level, monster, x, y))
                    {
                        continue;
                    }
                }

                level.Actors.Add(monster);
                level.Scheduler.Register(monster);
            }
        }
    }

    /// <summary>
    /// Tile-attuned monster system, design spec sections 14/26-38: in a special-floor room,
    /// each pick has a FloorAttunedSpawnChance chance of drawing a monster attuned to that
    /// room's own SpecialFloorType instead of an ordinary level-appropriate one -- placed
    /// directly on one of the room's own matching-FloorType tiles, never just anywhere in the
    /// room. Returns null (letting the caller fall back to its normal any-monster/any-tile
    /// spawn) for a plain room, on the FloorAttunedSpawnChance roll failing, or when the room
    /// has no unoccupied matching-FloorType tile left to place one on -- ordinary monsters are
    /// always still allowed in a special-floor room (section 37), and a floor-attuned monster
    /// is never conjured a floor type it can't actually stand on (section 15/38).
    /// </summary>
    private static Monster TrySpawnFloorAttunedMonster(Level level, Room room, int difficultyLevel, Random rng)
    {
        if (!room.IsSpecialFloorRoom || rng.NextDouble() >= FloorAttunementConfig.FloorAttunedSpawnChance)
        {
            return null;
        }

        var matchingTile = FindRandomMatchingFloorTile(level, room, room.SpecialFloorType, rng);
        return matchingTile.HasValue
            ? Monster.CreateFloorAttuned(matchingTile.Value.X, matchingTile.Value.Y, difficultyLevel, room.SpecialFloorType, rng)
            : null;
    }

    private static (int X, int Y)? FindRandomMatchingFloorTile(Level level, Room room, FloorType floorType, Random rng)
    {
        var candidates = new List<(int X, int Y)>();
        for (int x = room.X + 1; x < room.X + room.Width - 1; x++)
        {
            for (int y = room.Y + 1; y < room.Y + room.Height - 1; y++)
            {
                if (level.IsWalkable(x, y) && level.GetActorAt(x, y) == null && level.Tiles[x, y].FloorType == floorType)
                {
                    candidates.Add((x, y));
                }
            }
        }
        return candidates.Count == 0 ? null : candidates[rng.Next(candidates.Count)];
    }

    /// <summary>Items.All grouped by ItemType, computed once -- backs the weighted draw in SpawnItems so ambient floor items are balanced by type (see ItemTypeWeights) instead of a plain uniform draw that Scroll/Spellbook (one entry per spell) would dominate.</summary>
    private static readonly Dictionary<ItemType, List<Item>> ItemsByType =
        Items.All.GroupBy(i => i.Type).ToDictionary(g => g.Key, g => g.ToList());

    private static void SpawnItems(Level level, List<Room> rooms, Random rng)
    {
        foreach (var room in rooms)
        {
            if (rng.NextDouble() > 0.5)
            {
                continue;
            }

            if (room.Width <= 2 || room.Height <= 2)
            {
                continue;
            }

            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);

            if (level.IsBlockedForObjectPlacement(x, y))
            {
                continue;
            }

            var type = ItemTypeWeights.PickType(ItemsByType.Keys, rng);
            if (type == null)
            {
                continue; // unreachable in practice -- Items.All is never empty
            }
            var pool = ItemsByType[type.Value];
            var item = pool[rng.Next(pool.Count)];
            if (item.RequiresUniqueInstance)
            {
                // Charged items need their own independent charge count, an unidentified item
                // needs its own independent IsIdentified flag, and a container item needs its
                // own independent Contents list -- none of those can share the catalog
                // reference. See Item.RequiresUniqueInstance's own doc comment.
                item = item.Clone();
            }
            level.AddItem(x, y, item);
        }
    }

    /// <summary>Locked chests (Pick Lock) -- new dungeon content, no analog before this. Rarer than a plain item spawn since a chest holds a guaranteed item behind a skill check rather than a coin-flip find.</summary>
    private static void SpawnChests(Level level, List<Room> rooms, Random rng, int difficultyLevel)
    {
        foreach (var room in rooms)
        {
            if (rng.NextDouble() > 0.15)
            {
                continue;
            }

            if (room.Width <= 2 || room.Height <= 2)
            {
                continue;
            }

            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);

            if (level.IsBlockedForObjectPlacement(x, y))
            {
                continue;
            }

            var contents = new List<Item>();
            var type = ItemTypeWeights.PickType(ItemsByType.Keys, rng);
            if (type != null)
            {
                var pool = ItemsByType[type.Value];
                var item = pool[rng.Next(pool.Count)];
                contents.Add(item.RequiresUniqueInstance ? item.Clone() : item);
            }

            // Most chests are locked -- an unlocked one is a small, free bonus find.
            bool isLocked = rng.NextDouble() < 0.7;
            // Raised from the original 8 base -- GameLoop's pick roll only adds half of
            // Knowledge now, so this keeps failure a real possibility instead of a near-lock.
            int difficulty = 15 + (difficultyLevel / 2);
            level.Chests.Add(new Chest(x, y, isLocked, difficulty, contents));
        }
    }

    /// <summary>
    /// Where a straight 1-wide corridor crosses `room`'s rectangle boundary -- the one tile
    /// in this generator that behaves like a real doorway (a single carved tile with the
    /// room's much wider interior on one side, walls flanking the other three, since only a
    /// 1-wide strip was carved outside the room). `fixedCoord` is the corridor's constant
    /// coordinate (Y for a horizontal corridor, X for a vertical one); `towardCoord` is the
    /// coordinate on the far end the corridor runs toward/from, used only to tell which side
    /// of the room it approaches from. Returns null if the corridor doesn't cross this room's
    /// boundary in a simple single-tile way (e.g. it originates from inside the room's own
    /// span on that axis).
    /// </summary>
    private static (int X, int Y)? FindRoomEntrance(Room room, int fixedCoord, int towardCoord, bool corridorIsVertical)
    {
        if (corridorIsVertical)
        {
            int x = fixedCoord;
            if (x < room.X || x >= room.X + room.Width)
            {
                return null;
            }
            if (towardCoord < room.Y)
            {
                return (x, room.Y - 1);
            }
            if (towardCoord >= room.Y + room.Height)
            {
                return (x, room.Y + room.Height);
            }
            return null;
        }

        int y = fixedCoord;
        if (y < room.Y || y >= room.Y + room.Height)
        {
            return null;
        }
        if (towardCoord < room.X)
        {
            return (room.X - 1, y);
        }
        if (towardCoord >= room.X + room.Width)
        {
            return (room.X + room.Width, y);
        }
        return null;
    }

    private static void TrySpawnDoorAtEntrance(Level level, Room room, int fixedCoord, int towardCoord, bool corridorIsVertical, Random rng, int difficultyLevel)
    {
        var entrance = FindRoomEntrance(room, fixedCoord, towardCoord, corridorIsVertical);
        if (entrance == null || !level.IsInBounds(entrance.Value.X, entrance.Value.Y))
        {
            return;
        }

        // If `room` turned out to be dark, RollDarkRooms already force-placed a door here (and
        // at every other entrance it found, not just this formal one) -- SpawnDoor's own
        // GetDoorAt guard makes calling it again here a harmless no-op either way.
        SpawnDoor(level, entrance.Value.X, entrance.Value.Y, corridorIsVertical, rng, difficultyLevel);
    }

    /// <summary>
    /// True only if a single-tile door at (x, y) would form a genuine chokepoint -- both
    /// perpendicular neighbors still solid wall. FindRoomEntrance only reasons about ONE
    /// room/corridor pair -- it can't see that a completely unrelated nearby room or corridor
    /// happens to run alongside this exact tile, so this can legitimately fail even for a
    /// perfectly valid entrance. Without it, a door could end up with open floor on both
    /// perpendicular sides too, meaning the player (or line of sight) could simply go around it
    /// instead of ever needing to pass through it -- the one shape a Door can actually block
    /// anything through. Shared by SpawnDoor's own placement check and RollDarkRooms'
    /// eligibility check (a room can't safely be darkened if this fails at any of its entrances).
    /// </summary>
    private static bool IsValidDoorChokepoint(Level level, int x, int y, bool corridorIsVertical) =>
        corridorIsVertical
            ? !level.IsWalkable(x - 1, y) && !level.IsWalkable(x + 1, y)
            : !level.IsWalkable(x, y - 1) && !level.IsWalkable(x, y + 1);

    /// <summary>
    /// A Door has a chance to spawn at a genuine room entrance (see FindRoomEntrance/
    /// TrySpawnDoorAtEntrance) -- most entrances stay open, unless forceDoor overrides that (a
    /// dark room's entrance -- see RollDarkRooms). A spawned door is locked well under half the
    /// time (lower than Chest's own 0.7 -- locked doors sit on the floor's one and only
    /// route through, so they were coming up noticeably more often in practice than a chest);
    /// an unlocked one still needs one bump to open but
    /// never gates that on a roll/skill/key. IsPickable is rolled independently as a
    /// shortcut for classes with Pick Lock + lockpicks, but a locked door is ALWAYS
    /// bashable -- since this generator only ever produces a single linear chain of rooms
    /// (no side paths), every door sits on the one and only route through the floor,
    /// so a door that was neither pickable nor bashable would permanently strand any
    /// character without a Skeleton Key in hand. Bashing costs no skill or item (see
    /// GameLoop.BashDoor), so forcing it guarantees the floor is always solvable no
    /// matter the class, skills, or inventory -- a Skeleton Key just lets you skip the wait.
    /// </summary>
    private static void SpawnDoor(Level level, int x, int y, bool corridorIsVertical, Random rng, int difficultyLevel, bool forceDoor = false)
    {
        if (!forceDoor && rng.NextDouble() > 0.25)
        {
            return;
        }

        if (!level.IsWalkable(x, y) || level.GetItemAt(x, y) != null || level.GetChestAt(x, y) != null || level.GetDoorAt(x, y) != null)
        {
            return;
        }

        if (!IsValidDoorChokepoint(level, x, y, corridorIsVertical))
        {
            return;
        }

        bool isLocked = rng.NextDouble() < 0.35;
        bool isPickable = rng.NextDouble() < 0.75;
        // Forced true whenever locked -- see the type-level comment above for why an
        // unbashable, unpickable, keyless door can never be allowed to exist here.
        // Meaningless when unlocked (HandleDoorBump never even reads it in that case).
        bool isBashable = isLocked || rng.NextDouble() < 0.75;
        // Raised from the original 8 base -- see the matching comment in SpawnChests.
        int difficulty = 15 + (difficultyLevel / 2);
        int maxHealth = 20 + (difficultyLevel * 3);
        level.Doors.Add(new Door(x, y, isLocked, isPickable, isBashable, difficulty, maxHealth));
    }

    /// <summary>Hidden traps (Detect Traps) -- new dungeon content, no analog before this.</summary>
    private static void SpawnTraps(Level level, List<Room> rooms, Random rng, int difficultyLevel)
    {
        foreach (var room in rooms)
        {
            if (rng.NextDouble() > 0.2)
            {
                continue;
            }

            if (room.Width <= 2 || room.Height <= 2)
            {
                continue;
            }

            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);

            if (!level.IsWalkable(x, y) || level.GetItemAt(x, y) != null || level.GetChestAt(x, y) != null || level.GetDoorAt(x, y) != null)
            {
                continue;
            }

            int damage = 3 + (difficultyLevel / 3);
            level.Traps.Add(new Trap(x, y, damage));
        }
    }

    private static readonly (int Dx, int Dy)[] PushDirections =
    {
        (0, -1), (0, 1), (-1, 0), (1, 0),
        (-1, -1), (-1, 1), (1, -1), (1, 1)
    };

    /// <summary>A movable object generated with no legal push direction at all would be a permanently inert piece of set dressing masquerading as interactive -- SpawnRoomObjects skips placing one wherever this comes back false.</summary>
    private static bool HasAnyLegalPushDirection(Level level, int x, int y) =>
        PushDirections.Any(d => !level.IsBlockedForObjectPlacement(x + d.Dx, y + d.Dy));

    private static RoomObjectType PickCommonRoomObjectType(Random rng) => rng.Next(4) switch
    {
        0 => RoomObjectType.WaterFountain,
        1 => RoomObjectType.Shrine,
        2 => RoomObjectType.Boulder,
        _ => RoomObjectType.Statue
    };

    /// <summary>
    /// Substantial environmental objects (fountains, shrines, boulders, statues) -- new dungeon
    /// content, no analog before this. Conservative per-room rate, one candidate tile per room
    /// (mirrors SpawnChests/SpawnTraps' own shape). Rooms are required to have a couple of spare
    /// interior tiles beyond the one being claimed (Width/Height &gt; 4) so a single placement can
    /// never be the one tile standing between two halves of this generator's otherwise-open
    /// rectangular rooms -- no full connectivity search needed for that guarantee to hold.
    /// Suggested per-type defaults from the design spec: fountains and shrines are always
    /// stationary (they're landmarks, not puzzle pieces); boulders are usually movable; statues
    /// are usually stationary, with a minority rolled movable to keep "a statue might be a puzzle
    /// piece" a genuine, not-always-telegraphed possibility outside the dedicated puzzle arrangement below.
    /// </summary>
    private const double RoomObjectSpawnChance = 0.12;
    private const double MovableBoulderChance = 0.8;
    private const double MovableStatueChance = 0.2;
    private const double PuzzleStatueChance = 0.15;

    private static void SpawnRoomObjects(Level level, List<Room> rooms, Random rng)
    {
        // The puzzle arrangement gets its own low-probability roll and claims one whole room
        // outright, before the plain per-room roll below ever runs against it -- a statue that's
        // secretly wired to a hidden door must never also be eligible to roll as an ordinary,
        // independently-placed piece of set dressing.
        Room puzzleRoom = null;
        if (rooms.Count > 0 && rng.NextDouble() < PuzzleStatueChance)
        {
            puzzleRoom = TrySpawnPuzzleStatue(level, rooms, rng);
        }

        foreach (var room in rooms)
        {
            if (room == puzzleRoom || room.Width <= 4 || room.Height <= 4)
            {
                continue;
            }
            if (rng.NextDouble() > RoomObjectSpawnChance)
            {
                continue;
            }

            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);
            if (level.IsBlockedForObjectPlacement(x, y))
            {
                continue;
            }

            var type = PickCommonRoomObjectType(rng);
            bool isMovable = type switch
            {
                RoomObjectType.Boulder => rng.NextDouble() < MovableBoulderChance,
                RoomObjectType.Statue => rng.NextDouble() < MovableStatueChance,
                _ => false
            };
            if (isMovable && !HasAnyLegalPushDirection(level, x, y))
            {
                continue; // would be a permanently inert "movable" object -- skip the tile entirely rather than silently downgrading it to stationary
            }

            level.RoomObjects.Add(RoomObjectCatalog.Create(type, x, y, isMovable));
        }
    }

    /// <summary>
    /// A complete, guaranteed-solvable statue puzzle, generated as one coordinated arrangement
    /// rather than independent random rolls (design spec requirement). Finds a room with a wall
    /// segment that is a genuine chokepoint (see IsValidDoorChokepoint) between it and an
    /// already-carved, already-reachable neighboring space -- the far side is real floor, just
    /// not yet doored -- then places a movable statue exactly two tiles in from that wall along
    /// the perpendicular axis, with a trigger that reveals a door at the wall the moment the
    /// statue is pushed onto the one tile directly against it. A single push from the outer tile
    /// always solves it. Because the far side already exists and is already reachable by the
    /// normal route, the revealed door is purely a bonus shortcut -- solvability of the floor
    /// never depends on this puzzle ever being found or solved. Returns the room the puzzle was
    /// placed in, or null if no floor in this dungeon happened to offer a valid wall segment.
    /// </summary>
    private static Room TrySpawnPuzzleStatue(Level level, List<Room> rooms, Random rng)
    {
        var candidateRooms = rooms.Where(r => r.Width > 4 && r.Height > 4).OrderBy(_ => rng.Next()).ToList();
        foreach (var room in candidateRooms)
        {
            // Try each of the room's four walls, one random tile per wall per attempt.
            var wallOptions = new List<(int X, int Y, int InwardDx, int InwardDy, bool CorridorIsVertical)>
            {
                (rng.Next(room.X, room.X + room.Width), room.Y - 1, 0, 1, true),
                (rng.Next(room.X, room.X + room.Width), room.Y + room.Height, 0, -1, true),
                (room.X - 1, rng.Next(room.Y, room.Y + room.Height), 1, 0, false),
                (room.X + room.Width, rng.Next(room.Y, room.Y + room.Height), -1, 0, false)
            };

            foreach (var wall in wallOptions.OrderBy(_ => rng.Next()))
            {
                if (!level.IsInBounds(wall.X, wall.Y) || level.IsWalkable(wall.X, wall.Y) || level.GetDoorAt(wall.X, wall.Y) != null)
                {
                    continue;
                }
                int outerX = wall.X - wall.InwardDx, outerY = wall.Y - wall.InwardDy;
                if (!level.IsWalkable(outerX, outerY))
                {
                    continue; // nothing real on the far side -- this is just an exterior wall
                }
                if (!IsValidDoorChokepoint(level, wall.X, wall.Y, wall.CorridorIsVertical))
                {
                    continue;
                }

                var target = (X: wall.X + wall.InwardDx, Y: wall.Y + wall.InwardDy);
                var statueStart = (X: target.X + wall.InwardDx, Y: target.Y + wall.InwardDy);
                var playerStand = (X: statueStart.X + wall.InwardDx, Y: statueStart.Y + wall.InwardDy);

                bool InsideInterior((int X, int Y) p) =>
                    p.X > room.X && p.X < room.X + room.Width - 1 && p.Y > room.Y && p.Y < room.Y + room.Height - 1;

                if (!InsideInterior(target) || !InsideInterior(statueStart) || !InsideInterior(playerStand))
                {
                    continue;
                }
                if (level.IsBlockedForObjectPlacement(target.X, target.Y) || level.IsBlockedForObjectPlacement(statueStart.X, statueStart.Y)
                    || level.IsBlockedForActorMovement(playerStand.X, playerStand.Y))
                {
                    continue;
                }

                var trigger = new RoomObjectTrigger(
                    RoomObjectTriggerCondition.ReachedPosition, RoomObjectTriggerEffect.RevealHiddenDoor,
                    trackedPosition: target, hiddenDoorPosition: (wall.X, wall.Y));
                var statue = RoomObjectCatalog.Create(RoomObjectType.Statue, statueStart.X, statueStart.Y, isMovable: true);
                statue.Trigger = trigger;
                level.RoomObjects.Add(statue);
                return room;
            }
        }
        return null;
    }

    /// <summary>Guarantees one Skeleton Key somewhere on any floor with at least one locked Door, so no class/skill combination is ever soft-locked behind one -- see Items.SkeletonKey's doc comment for why it's never drawn from the general item pool instead.</summary>
    private static void SpawnSkeletonKeys(Level level, List<Room> rooms, Random rng)
    {
        if (!level.Doors.Any(d => d.IsLocked))
        {
            return;
        }

        var candidateRooms = rooms.Where(r => r.Width > 2 && r.Height > 2).ToList();
        if (candidateRooms.Count == 0)
        {
            return;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var room = candidateRooms[rng.Next(candidateRooms.Count)];
            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);

            if (level.IsBlockedForObjectPlacement(x, y))
            {
                continue;
            }

            // Cloned, not the shared catalog reference -- Charges is now per-instance mutable
            // state (see Items.SkeletonKey's own doc comment), same rule every other charged
            // item's spawn site already follows.
            level.AddItem(x, y, Items.SkeletonKey.Clone());
            return;
        }
    }

    /// <summary>
    /// Places at most one Trader on the floor -- forceSpawn (driven by DungeonManager's
    /// LevelsSinceLastTrader counter) overrides the plain random chance to guarantee the
    /// design's "at least one trader every 3 levels" requirement. Picking from `rooms` gets
    /// two requirements for free: the boss room is never in that list (see TrySpawnBossRoom's
    /// own comment), so a trader can never land there, and every room in `rooms` is already
    /// guaranteed reachable by this generator's own connectivity guarantee, so no separate
    /// accessibility check is needed either. Explicitly excludes both stairs tiles, which
    /// -- unlike a boss room -- aren't otherwise structurally impossible to land on.
    /// </summary>
    /// <summary>
    /// A Trader never moves (see Trader/NPC's own doc comments) and fully occupies its tile
    /// (level.GetActorAt treats it like any other blocking actor) -- spawning one directly
    /// beside a door would let it permanently block the one chokepoint through that door,
    /// with no way to route around it. Checked in all 8 directions, not just the 4 a corridor
    /// could approach from, since a diagonal-adjacent trader sitting right at a doorway's
    /// threshold would be just as much of a problem.
    /// </summary>
    private static bool IsAdjacentToADoor(Level level, int x, int y)
    {
        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if ((dx != 0 || dy != 0) && level.GetDoorAt(x + dx, y + dy) != null)
                {
                    return true;
                }
            }
        }
        return false;
    }

    private static void SpawnTrader(Level level, List<Room> rooms, Random rng, int difficultyLevel, bool forceSpawn)
    {
        if (!forceSpawn && rng.NextDouble() > TraderConfig.TraderSpawnChance)
        {
            return;
        }

        var candidateRooms = rooms.Where(r => r.Width > 2 && r.Height > 2).ToList();
        if (candidateRooms.Count == 0)
        {
            return;
        }

        for (int attempt = 0; attempt < 10; attempt++)
        {
            var room = candidateRooms[rng.Next(candidateRooms.Count)];
            int x = rng.Next(room.X + 1, room.X + room.Width - 1);
            int y = rng.Next(room.Y + 1, room.Y + room.Height - 1);

            if (!level.IsWalkable(x, y) || level.GetActorAt(x, y) != null || level.GetDoorAt(x, y) != null
                || level.GetChestAt(x, y) != null || level.GetTrapAt(x, y) != null || level.GetRoomObjectAt(x, y) != null
                || (x, y) == level.StairsUpPosition || (x, y) == level.StairsDownPosition
                || IsAdjacentToADoor(level, x, y))
            {
                continue;
            }

            var trader = Trader.CreateRandom(x, y, difficultyLevel, rng);
            // Deliberately never Scheduler.Register'd -- see Trader/NPC's own doc comments.
            // A trader is fully visible/blocking/interactable via level.Actors alone; it's
            // simply never offered a turn, which is what makes it stationary.
            level.Actors.Add(trader);
            return;
        }
    }
}
