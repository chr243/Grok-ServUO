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
        public static TimeSpan StuckTimeout = TimeSpan.FromSeconds(20.0);

        /// <summary>Hard cap — never leave the station blocked longer than this for one leg.</summary>
        public static TimeSpan MaxTravelDuration = TimeSpan.FromMinutes(3.0);

        /// <summary>Default time spent performing the job at the resource.</summary>
        public static TimeSpan DefaultWorkDuration = TimeSpan.FromSeconds(12.0);

        /// <summary>Default reward stack size for the Earth gathering job.</summary>
        public static int DefaultGatherRewardAmount = 3;

        /// <summary>Animation / action delay while working (visual only).</summary>
        public static TimeSpan WorkAnimInterval = TimeSpan.FromSeconds(2.0);

        /// <summary>Timer tick for station job processing.</summary>
        public static TimeSpan JobTickInterval = TimeSpan.FromSeconds(1.0);

        /// <summary>Stop auto-loop when station holds at least this much Iron Ore.</summary>
        public static int MaxStoredOre = 1000;
    }
}

