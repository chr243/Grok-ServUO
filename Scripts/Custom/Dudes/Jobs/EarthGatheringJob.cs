using System;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// First gathering job: Earth Dudes mine nearest valid ore tile (real world harvestables).
    /// </summary>
    public sealed class EarthGatheringJob : GatheringJob
    {
        public EarthGatheringJob()
            : base("earth_gather", "Earth Gathering")
        {
        }

        public override bool CanPerform(DudeData data)
        {
            return data != null && data.Type == DudeType.Earth;
        }

        public override string GetResourceLabel()
        {
            return "Ore";
        }

        public override Type GetStoredRewardType()
        {
            return typeof(BaseOre);
        }

        protected override Item CreateGatheredItem(DudeData data)
        {
            double skill = DudeJobHarvest.GetEffectiveSkill(data);
            Item item = DudeJobHarvest.CreateFromVeins(Mining.System.OreAndStone, skill, 1);
            if (item != null)
                return item;
            return new IronOre(1);
        }

        public override bool CountsTowardStorage(Item item)
        {
            return item is BaseOre;
        }

        public override bool TryFindDestination(DudeJobStation station, DudeData data, out Point3D destination, out int distance)
        {
            destination = Point3D.Zero;
            distance = 0;

            if (station == null || station.Deleted || station.Map == null || station.Map == Map.Internal)
                return false;

            Map map = station.Map;
            Point3D origin = station.Location;
            HarvestDefinition def = Mining.System.OreAndStone;
            int radius = DudeJobConfig.SearchRadius;

            Point3D best = Point3D.Zero;
            int bestDist = int.MaxValue;
            bool found = false;

            // Expanding ring search — prefer nearer valid mineable land tiles.
            for (int r = 1; r <= radius; r++)
            {
                for (int dx = -r; dx <= r; dx++)
                {
                    for (int dy = -r; dy <= r; dy++)
                    {
                        if (Math.Abs(dx) != r && Math.Abs(dy) != r)
                            continue;

                        int x = origin.X + dx;
                        int y = origin.Y + dy;

                        LandTile lt = map.Tiles.GetLandTile(x, y);
                        int tileId = lt.ID & 0x3FFF;

                        if (!def.Validate(tileId) && !def.Validate(lt.ID))
                            continue;

                        int z = lt.Z;
                        Point3D candidate = new Point3D(x, y, z);
                        int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));

                        if (dist < bestDist)
                        {
                            bestDist = dist;
                            best = candidate;
                            found = true;
                        }
                    }
                }

                if (found && bestDist <= r)
                    break;
            }

            if (!found)
                return false;

            destination = best;
            distance = bestDist;
            return true;
        }
    }
}
