using System;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Base for gathering-style jobs (search world harvestables → collect → return).
    /// </summary>
    public abstract class GatheringJob : DudeJob
    {
        protected GatheringJob(string id, string name)
            : base(id, name)
        {
        }

        public override int GetWorkAnimation(DudeData data)
        {
            return 11;
        }

        /// <summary>Harvest definition whose banks this job depletes (mining/lumber/fish).</summary>
        protected abstract HarvestDefinition GetHarvestDefinition();

        public override Item CreateReward(DudeData data)
        {
            return CreateReward(data, null, Point3D.Zero, null);
        }

        public override Item CreateReward(DudeData data, Map map, Point3D loc, Mobile harvester)
        {
            Item reward = CreateGatheredItem(data, map, loc, harvester);
            if (reward == null)
                return null;

            // HarvestAt already sets stack size from the bank; only bump if still at 1 and config wants more
            // and we did not go through a bank (fallback path).
            if (map == null || map == Map.Internal || harvester == null)
            {
                int amount = GetRewardAmount(data);
                if (reward.Stackable && amount > reward.Amount)
                    reward.Amount = amount;
            }

            DudeJobHarvest.TryGainGatherSkill(data);
            return reward;
        }

        protected abstract Item CreateGatheredItem(DudeData data, Map map, Point3D loc, Mobile harvester);

        protected virtual int GetRewardAmount(DudeData data)
        {
            return DudeJobConfig.DefaultGatherRewardAmount;
        }
    }
}
