using System;
using Server.Items;

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

        public override Item CreateReward(DudeData data)
        {
            Item reward = CreateGatheredItem(data);
            if (reward == null)
                return null;

            int amount = GetRewardAmount(data);
            if (reward.Stackable && amount > 1)
                reward.Amount = amount;

            return reward;
        }

        protected abstract Item CreateGatheredItem(DudeData data);

        protected virtual int GetRewardAmount(DudeData data)
        {
            return DudeJobConfig.DefaultGatherRewardAmount;
        }
    }
}
