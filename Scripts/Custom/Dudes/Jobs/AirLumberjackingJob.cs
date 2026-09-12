using System;
using Server.Engines.Harvest;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Air Dudes lumberjack nearest valid tree (static harvestables), same loop as Earth mining.
    /// </summary>
    public sealed class AirLumberjackingJob : GatheringJob
    {
        public AirLumberjackingJob()
            : base("air_lumber", "Air Lumberjacking")
        {
        }

        public override bool CanPerform(DudeData data)
        {
            return data != null && data.Type == DudeType.Air;
        }

        public override string GetResourceLabel()
        {
            return "Logs";
        }

        public override Type GetStoredRewardType()
        {
            return typeof(BaseLog);
        }

        public override int GetWorkAnimation(DudeData data)
        {
            return 13; // chop
        }

        public override int GetWorkSound(DudeData data)
        {
            return 0x13E;
        }

        protected override HarvestDefinition GetHarvestDefinition()
        {
            return Lumberjacking.System.Definition;
        }

        protected override Item CreateGatheredItem(DudeData data, Map map, Point3D loc, Mobile harvester)
        {
            double skill = DudeJobHarvest.GetEffectiveSkill(data);
            HarvestDefinition def = GetHarvestDefinition();

            if (map != null && map != Map.Internal && harvester != null)
            {
                Item harvested = DudeJobHarvest.HarvestAt(def, map, loc, harvester, skill, DudeJobConfig.DefaultGatherRewardAmount);
                if (harvested != null)
                    return harvested;
            }

            Item item = DudeJobHarvest.CreateFromVeins(def, skill, DudeJobConfig.DefaultGatherRewardAmount);
            if (item != null)
                return item;
            return new Log(DudeJobConfig.DefaultGatherRewardAmount);
        }

        public override bool CountsTowardStorage(Item item)
        {
            return item is BaseLog;
        }

        public override bool TryFindDestination(DudeJobStation station, DudeData data, out Point3D destination, out int distance)
        {
            destination = Point3D.Zero;
            distance = 0;

            if (station == null || station.Deleted || station.Map == null || station.Map == Map.Internal)
                return false;

            Map map = station.Map;
            Point3D origin = station.Location;
            HarvestDefinition def = GetHarvestDefinition();
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

                        StaticTile[] tiles = map.Tiles.GetStaticTiles(x, y, false);
                        for (int i = 0; i < tiles.Length; i++)
                        {
                            StaticTile tile = tiles[i];
                            int id = (tile.ID & 0x3FFF) | 0x4000;

                            if (!def.Validate(id) && !def.Validate(tile.ID) && !def.Validate(tile.ID & 0x3FFF))
                                continue;

                            if (!DudeJobHarvest.HasResources(def, map, x, y))
                                continue;

                            Point3D candidate = new Point3D(x, y, tile.Z);
                            int dist = (int)Math.Sqrt((dx * dx) + (dy * dy));

                            if (dist < bestDist)
                            {
                                bestDist = dist;
                                best = candidate;
                                found = true;
                            }
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
