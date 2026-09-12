using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Abstract job a Dude can perform at a Job Station.
    /// Stations ask the registry / Dude what job it can do — they do not hard-code job lists.
    /// </summary>
    public abstract class DudeJob
    {
        private readonly string m_Id;
        private readonly string m_Name;

        protected DudeJob(string id, string name)
        {
            m_Id = id;
            m_Name = name;
        }

        public string Id { get { return m_Id; } }
        public string Name { get { return m_Name; } }

        /// <summary>Whether this Dude (by data) can perform this job.</summary>
        public abstract bool CanPerform(DudeData data);

        /// <summary>
        /// Find a destination near the station. Returns false if none found.
        /// </summary>
        public abstract bool TryFindDestination(DudeJobStation station, DudeData data, out Point3D destination, out int distance);

        /// <summary>Time spent working at the destination (separate from travel).</summary>
        public virtual TimeSpan GetWorkDuration(DudeData data)
        {
            return DudeJobConfig.DefaultWorkDuration;
        }

        /// <summary>Animation action ID while working (BaseCreature.Animate).</summary>
        public virtual int GetWorkAnimation(DudeData data)
        {
            return 11; // classic digging / work
        }

        public virtual int GetWorkSound(DudeData data)
        {
            return 0x125;
        }

        /// <summary>Create the reward item(s) deposited into the station. May return null.</summary>
        public abstract Item CreateReward(DudeData data);

        /// <summary>Human-readable resource label for UI.</summary>
        public abstract string GetResourceLabel();

        /// <summary>Primary stacked reward type used for station storage caps (null = no cap).</summary>
        public abstract System.Type GetStoredRewardType();

        /// <summary>Whether an item counts toward this job's station storage cap.</summary>
        public virtual bool CountsTowardStorage(Item item)
        {
            System.Type t = GetStoredRewardType();
            return item != null && t != null && t.IsInstanceOfType(item);
        }

        /// <summary>Estimate outbound/return travel time from distance.</summary>
        public virtual TimeSpan EstimateTravelTime(int distance)
        {
            double speed = DudeJobConfig.TravelTilesPerSecond;
            if (speed <= 0.0)
                speed = 1.0;

            double seconds = distance / speed;
            TimeSpan span = TimeSpan.FromSeconds(seconds);

            if (span > DudeJobConfig.MaxTravelDuration)
                span = DudeJobConfig.MaxTravelDuration;

            if (span < TimeSpan.FromSeconds(2.0))
                span = TimeSpan.FromSeconds(2.0);

            return span;
        }
    }
}
