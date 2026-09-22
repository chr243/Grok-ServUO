namespace Server.Custom.Dudes
{
    /// <summary>
    /// Summon/despawn particle family. Decouples VFX from the type switch so a new type
    /// just picks a family (including Reuse over an existing one).
    /// </summary>
    public enum DudeVfx
    {
        Fire = 0,
        Water = 1,
        Earth = 2,
        Air = 3,
        Poison = 4
    }
}
