namespace BENEATH_FORGOTTEN_STONE.Entities.Projectiles;

/// <summary>Tunable constants for projectile flight -- mirrors BossConfig/TraderConfig's plain-constants style.</summary>
public static class ProjectileConfig
{
    /// <summary>Wall-clock delay between animation frames while a projectile travels. This is the first Thread.Sleep in the codebase -- safe here specifically because GameLoop.Run's single-threaded loop can't advance anything else while this call blocks, which is exactly the "pause all other game time" requirement.</summary>
    public const int AnimationDelayMs = 40;
}
