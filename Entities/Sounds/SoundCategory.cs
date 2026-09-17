namespace BENEATH_FORGOTTEN_STONE.Entities.Sounds;

/// <summary>
/// What kind of thing produced a candidate sound event -- drives selection weighting
/// (SoundConfig's per-category weights) and which catalog/position source feeds it.
/// Environment and Boss both have a real map position and are range-gated; CreatureAmbient
/// represents the level's population generally and has no position at all (see design spec
/// sections 2/4/6). Special is reserved for future one-off scripted events, not used yet.
/// </summary>
public enum SoundCategory
{
    Environment,
    CreatureAmbient,
    Boss,
    Special
}
