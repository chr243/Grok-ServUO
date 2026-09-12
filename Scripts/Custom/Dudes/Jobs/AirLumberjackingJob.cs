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
            return typeof(Log);
        }

        public override int GetWorkAnimation(DudeData data)
        {
            return 13; // chop
        }

        public override int GetWorkSound(DudeData data)
        {
            return 0x13E;
        }

        protected override Item CreateGatheredItem(DudeData data)
        {
            return new Log(1);
        }

        public override bool TryFindDestination(DudeJobStation station, DudeData data, out Point3D destination, out int distance)
        {
            destination = Point3D.Zero;
            distance = 0;

            if (station == null || station.Deleted || station.Map == null || station.Map == Map.Internal)
                return false;

            Map map = station.Map;
            Point3D origin = station.Location;
            HarvestDefinition def = Lumberjacking.System.Definition;
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
