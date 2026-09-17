using BENEATH_FORGOTTEN_STONE.Dungeon;

namespace BENEATH_FORGOTTEN_STONE.Entities.Spells;

/// <summary>
/// Target-resolution logic shared by SpellCaster and SkillCaster -- extracted
/// so both casting engines walk the exact same range/line-of-sight/AoE rules
/// instead of maintaining two copies, the same reasoning RayTracer already
/// exists for on the aiming side.
/// </summary>
public static class TargetResolver
{
    public static bool ResolveAffectedActors(SpellTargetingConfiguration targeting, SpellCastingContext context, out string failureReason)
    {
        failureReason = "";
        var caster = context.Caster;
        int range = targeting.EffectiveRange(context.RankScaling);

        switch (targeting.TargetType)
        {
            case SpellTargetType.Self:
                context.AffectedActors = new List<Actor> { caster };
                return true;

            case SpellTargetType.SingleTarget:
                // Pet and Companion System: friendly fire stays disabled -- a hostile spell/skill
                // aimed straight at the caster's own pet finds "no target" rather than hitting it.
                if (context.TargetActor == null || !context.TargetActor.CanBeTargeted || Allegiance.AreFriendly(caster, context.TargetActor))
                {
                    failureReason = "No target in range.";
                    return false;
                }
                if (Distance(caster.X, caster.Y, context.TargetActor.X, context.TargetActor.Y) > range)
                {
                    failureReason = "Target is out of range.";
                    return false;
                }
                if (targeting.RequiresLineOfSight &&
                    !HasLineOfSight(context.Level, (caster.X, caster.Y), (context.TargetActor.X, context.TargetActor.Y)))
                {
                    failureReason = "No line of sight to target.";
                    return false;
                }
                context.AffectedActors = new List<Actor> { context.TargetActor };
                return true;

            case SpellTargetType.Tile:
                if (context.TargetTile == null)
                {
                    failureReason = "No valid target tile.";
                    return false;
                }
                var (tx, ty) = context.TargetTile.Value;
                if (Distance(caster.X, caster.Y, tx, ty) > range)
                {
                    failureReason = "Target is out of range.";
                    return false;
                }
                // Pet and Companion System: excludes any actor friendly to the caster OTHER than
                // the caster itself, so an AoE never damages the caster's own pet -- "friendly
                // fire should remain disabled for pets in the initial implementation" -- while
                // leaving a caster's pre-existing ability to catch themselves in their own AoE
                // (e.g. a Tile spell aimed at their own feet) completely unchanged.
                bool IsFriendlyNonCaster(Actor a) => !ReferenceEquals(a, caster) && Allegiance.AreFriendly(caster, a);
                context.AffectedActors = targeting.AreaOfEffectRadius > 0
                    ? context.Level.Actors.Where(a => a.IsAlive && a.CanBeTargeted && !IsFriendlyNonCaster(a) && Distance(a.X, a.Y, tx, ty) <= targeting.AreaOfEffectRadius).ToList()
                    : context.Level.Actors.Where(a => a.IsAlive && a.CanBeTargeted && !IsFriendlyNonCaster(a) && a.X == tx && a.Y == ty).ToList();
                return true;

            case SpellTargetType.SelfCenteredArea:
                // No failure case, even with zero eligible actors -- mirrors Tile-AoE's own
                // "can legitimately hit nothing" tolerance. A skill that wants to reject an
                // empty result for free (Turn Undead: "no undead nearby") does so via
                // Skill.PreconditionCheck instead, reading this same AffectedActors list.
                context.AffectedActors = context.Level.Actors
                    .Where(a => a.IsAlive && a.CanBeTargeted && a != caster
                        && (targeting.CreatureTypeFilter == null || (a is Monster m && m.CreatureType == targeting.CreatureTypeFilter))
                        && Distance(caster.X, caster.Y, a.X, a.Y) <= targeting.AreaOfEffectRadius
                        && (!targeting.RequiresLineOfSight || HasLineOfSight(context.Level, (caster.X, caster.Y), (a.X, a.Y))))
                    .ToList();
                return true;

            case SpellTargetType.Item:
                // Resolved by the caller (GameLoop.HandleCastSpell) into context.TargetItem before
                // Cast() runs -- there's no Actor for this to populate AffectedActors with.
                if (context.TargetItem == null)
                {
                    failureReason = "No item selected.";
                    return false;
                }
                return true;

            default:
                return true;
        }
    }

    public static double Distance(int x1, int y1, int x2, int y2)
    {
        int dx = x1 - x2;
        int dy = y1 - y2;
        return Math.Sqrt((dx * dx) + (dy * dy));
    }

    /// <summary>Bresenham line walk checking tile transparency between two points -- independent of the player-FOV-only IsVisible flag, so it works symmetrically for monster casters too.</summary>
    public static bool HasLineOfSight(Level level, (int X, int Y) from, (int X, int Y) to)
    {
        int x0 = from.X, y0 = from.Y, x1 = to.X, y1 = to.Y;
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0);
        int sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1;
        int err = dx + dy;

        while (true)
        {
            bool isEndpoint = (x0 == from.X && y0 == from.Y) || (x0 == x1 && y0 == y1);
            if (!isEndpoint && level.IsInBounds(x0, y0) && !level.IsTransparent(x0, y0))
            {
                return false;
            }
            if (x0 == x1 && y0 == y1)
            {
                return true;
            }
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
}
