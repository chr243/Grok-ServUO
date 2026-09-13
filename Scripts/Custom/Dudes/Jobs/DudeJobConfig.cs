using System;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Tunables for job travel / work / reward. Change here — not hard-coded in station logic.
    /// </summary>
    public static class DudeJobConfig
    {
        /// <summary>How far (tiles) around the station to search for a resource.</summary>
        public static int SearchRadius = 40;

        /// <summary>Worker movement: approximate tiles per second used for ETA display.</summary>
        public static double TravelTilesPerSecond = 2.0;

        /// <summary>If the worker makes no progress for this long, teleport toward goal.</summary>
        public static TimeSpan StuckTimeout = TimeSpan.FromSeconds(3.0);

        /// <summary>Hard cap — never leave the station blocked longer than this for one leg.</summary>
        public static TimeSpan MaxTravelDuration = TimeSpan.FromMinutes(3.0);

        /// <summary>Default time spent performing the job at the resource.</summary>
        public static TimeSpan DefaultWorkDuration = TimeSpan.FromSeconds(12.0);

        /// <summary>Default reward stack size for gathering jobs.</summary>
        public static int DefaultGatherRewardAmount = 3;

        /// <summary>Animation / action delay while working (visual only).</summary>
        public static TimeSpan WorkAnimInterval = TimeSpan.FromSeconds(2.0);

        /// <summary>Timer tick for station job processing.</summary>
        public static TimeSpan JobTickInterval = TimeSpan.FromSeconds(1.0);

        /// <summary>
        /// Stop auto-loop when station holds at least this much of the job's primary resource.
        /// 0 or less = unlimited (job runs until stopped).
        /// </summary>
        public static int MaxStoredResource = 0;

        /// <summary>EXP awarded to the working Dude each completed job cycle.</summary>
        public static int JobCycleExp = 1;

        /// <summary>Chance (0–1) each completed cycle also yields Dude Dust (default 5%).</summary>
        public static double JobDustChance = 0.05;

        /// <summary>Dude Dust amount granted when the bonus roll succeeds.</summary>
        public static int JobDustAmount = 1;

        /// <summary>Starting gathering skill for a newly caught Dude (independent of combat level).</summary>
        public static double BaseGatherSkill = 0.0;

        /// <summary>Hard cap for Dude gathering skill.</summary>
        public static double MaxGatherSkill = 120.0;

        /// <summary>Base chance (0-1) to gain skill on a successful gather cycle at skill 0.</summary>
        public static double GatherSkillGainChance = 0.35;

        /// <summary>Skill points gained when a gather skill check succeeds (scales down at high skill).</summary>
        public static double GatherSkillGainAmount = 0.1;

        /// <summary>Legacy alias for MaxStoredResource (Iron Ore jobs).</summary>
        public static int MaxStoredOre
        {
            get { return MaxStoredResource; }
            set { MaxStoredResource = value; }
        }
    }
}
