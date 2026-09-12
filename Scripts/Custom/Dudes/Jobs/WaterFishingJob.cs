using System;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Water Dudes fish nearest valid water tile, same loop as Earth mining.
    /// </summary>
    public sealed class WaterFishingJob : GatheringJob
    {
        public WaterFishingJob()
            : base("water_fish", "Water Fishing")
        {
        }

        public override bool CanPerform(DudeData data)
        {
            return data != null && data.Type == DudeType.Water;
        }

        public override string GetResourceLabel()
        {
            return "Fish";
        }

        public override Type GetStoredRewardType()
        {
            return typeof(Fish);
        }

        public override int GetWorkAnimation(DudeData data)
        {
            return 12; // fish cast-ish
        }

        public override int GetWorkSound(DudeData data)
        {
            return 0x364;
        }

        protected override Item CreateGatheredItem(DudeData data)
        {
            return new Fish(1);
        }

        public override bool TryFindDestination(DudeJobStation station, DudeData data, out Point3D destination, out int distance)
        {
            destination = Point3D.Zero;
            distance = 0;

            if (station == null || station.Deleted || station.Map == null || station.Map == Map.Internal)
                return false;

            Map map = station.Map;
            Point3D origin = station.Location;
            HarvestDefinition def = Fishing.System.Definition;
            int radius = DudeJobConfig.SearchRadius;

            Point3D best = Point3D.Zero;
            int bestDist = int.MaxValue;
            bool found = false;

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
