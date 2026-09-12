namespace Server.Custom.Dudes.Spawning
{
    /// <summary>Which Dude pool a DudeSpawner draws from.</summary>
    public enum DudeSpawnPreset
    {
        /// <summary>Weighted mix of all catchable tiers (weak common → strong rare).</summary>
        All = 0,
        Weak = 1,
        Basic = 2,
        Medium = 3,
        Strong = 4,
        Fire = 5,
        Water = 6,
        Earth = 7,
        Air = 8,
        /// <summary>Weak + basic only (good for starter areas).</summary>
        Starter = 9
    }
}
