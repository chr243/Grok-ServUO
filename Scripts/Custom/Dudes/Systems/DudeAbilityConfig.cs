using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Per-ability live-tunable numerics. Persisted to Config/DudeAbilities.cfg.
    /// Edited in-game via [DudeAbilities — no restart / recompile required.
    /// </summary>
    public sealed class DudeAbilityTune
    {
        public string Id;

        /// <summary>Active cooldown seconds. 0 = passive / n/a.</summary>
        public double CooldownSeconds;

        public double StunSeconds;
        public double DurationSeconds;
        public double SpeedFactor;
        public double DamageVsBlast;
        public double HealHitsFraction;
        public int Radius;
        public double TickSeconds;
        public double HitChance;
        public double GapSeconds;
        public double StunMin;
        public double StunMax;
        public double ReduceSeconds;
        public double FloorSeconds;

        public DudeAbilityTune Clone()
        {
            DudeAbilityTune t = new DudeAbilityTune();
            t.Id = Id;
            t.CooldownSeconds = CooldownSeconds;
            t.StunSeconds = StunSeconds;
            t.DurationSeconds = DurationSeconds;
            t.SpeedFactor = SpeedFactor;
            t.DamageVsBlast = DamageVsBlast;
            t.HealHitsFraction = HealHitsFraction;
            t.Radius = Radius;
            t.TickSeconds = TickSeconds;
            t.HitChance = HitChance;
            t.GapSeconds = GapSeconds;
            t.StunMin = StunMin;
            t.StunMax = StunMax;
            t.ReduceSeconds = ReduceSeconds;
            t.FloorSeconds = FloorSeconds;
            return t;
        }
    }

    public static class DudeAbilityConfig
    {
        public static readonly string FilePath = Path.Combine("Config", "DudeAbilities.cfg");

        /// <summary>Shared blast scale used by GetBlastDamage (then * AbilityDamageMultiplier).</summary>
        public static int BlastBase = 8;
        public static int BlastPerLevel = 2;

        private static readonly Dictionary<string, DudeAbilityTune> m_Tunes =
            new Dictionary<string, DudeAbilityTune>(StringComparer.OrdinalIgnoreCase);

        private static bool m_Loaded;

        /// <summary>Canonical ability id order for gump / save.</summary>
        public static readonly string[] AbilityIds = new string[]
        {
            "blast", "tide_mend", "fault_strike", "tailwind_self",
            "ring_of_fire", "tide_chorus", "aftershock", "tailwind",
            "burn", "spring", "faultline", "slipstream"
        };

        public static void Configure()
        {
            Load();
        }

        public static void EnsureLoaded()
        {
            if (!m_Loaded)
                Load();
        }

        public static void Load()
        {
            m_Loaded = true;
            ResetDefaults();

            if (!File.Exists(FilePath))
                return;

            try
            {
                string[] lines = File.ReadAllLines(FilePath);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrEmpty(line))
                        continue;

                    line = line.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                        continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    ApplyKeyValue(key, val);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DudeAbilityConfig: failed to load {0}: {1}", FilePath, ex.Message);
            }
        }

        public static bool Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                using (StreamWriter writer = new StreamWriter(FilePath, false))
                {
                    writer.WriteLine("# Dude ability tunables — edited via [DudeAbilities");
                    writer.WriteLine("# Format: id.Field=value  (case-insensitive ids)");
                    writer.WriteLine("# Shared blast scale (GetBlastDamage):");
                    writer.WriteLine("BlastBase={0}", BlastBase);
                    writer.WriteLine("BlastPerLevel={0}", BlastPerLevel);
                    writer.WriteLine();

                    for (int i = 0; i < AbilityIds.Length; i++)
                    {
                        DudeAbilityTune t = Get(AbilityIds[i]);
                        if (t == null)
                            continue;

                        writer.WriteLine("# --- {0} ---", t.Id);
                        WriteTune(writer, t);
                        writer.WriteLine();
                    }
                }

                m_Loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DudeAbilityConfig: failed to save {0}: {1}", FilePath, ex.Message);
                return false;
            }
        }

        public static void ResetDefaults()
        {
            BlastBase = 8;
            BlastPerLevel = 2;
            m_Tunes.Clear();

            PutDefault("blast", 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            PutDefault("tide_mend", 10, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            PutDefault("fault_strike", 10, 1.0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            PutDefault("tailwind_self", 10, 0, 5.0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            PutDefault("ring_of_fire", 12, 0, 0, 0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0);
            PutDefault("tide_chorus", 12, 0, 0, 0, 0, 0.20, 0, 0, 0, 0, 0, 0, 0, 0);
            PutDefault("aftershock", 12, 1.0, 0, 0, 0.5, 0, 3, 0, 0, 0, 0, 0, 0, 0);
            PutDefault("tailwind", 12, 0, 5.0, 0.5, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0);

            PutDefault("burn", 0, 0, 0, 0, 0.3, 0, 0, 1.0, 0.5, 0, 0, 0, 0, 0);
            PutDefault("spring", 0, 0, 0, 0, 0, 0.05, 0, 2.0, 0, 0, 0, 0, 0, 0);
            PutDefault("faultline", 0, 0, 0, 0, 0.33, 0, 0, 0, 0, 10.0, 0.5, 1.0, 0, 0);
            PutDefault("slipstream", 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 2.0, 7.0);
        }

        public static void ResetAbilityDefaults(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            EnsureLoaded();

            // Rebuild one ability from a fresh defaults snapshot.
            Dictionary<string, DudeAbilityTune> backup = new Dictionary<string, DudeAbilityTune>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, DudeAbilityTune> kv in m_Tunes)
                backup[kv.Key] = kv.Value.Clone();

            int blastBase = BlastBase;
            int blastPer = BlastPerLevel;

            ResetDefaults();

            DudeAbilityTune fresh;
            if (m_Tunes.TryGetValue(id, out fresh))
            {
                backup[id] = fresh.Clone();
            }

            m_Tunes.Clear();
            foreach (KeyValuePair<string, DudeAbilityTune> kv in backup)
                m_Tunes[kv.Key] = kv.Value;

            BlastBase = blastBase;
            BlastPerLevel = blastPer;
        }

        public static DudeAbilityTune Get(string id)
        {
            EnsureLoaded();

            if (string.IsNullOrEmpty(id))
                return null;

            DudeAbilityTune t;
            if (m_Tunes.TryGetValue(id, out t))
                return t;

            // Unknown id — empty tune so callers never NRE.
            t = new DudeAbilityTune();
            t.Id = id;
            m_Tunes[id] = t;
            return t;
        }

        public static TimeSpan GetCooldown(string id)
        {
            DudeAbilityTune t = Get(id);
            if (t != null && t.CooldownSeconds > 0.0)
                return TimeSpan.FromSeconds(t.CooldownSeconds);
            return TimeSpan.Zero;
        }

        public static bool IsPassive(string id)
        {
            DudeAbilityTune t = Get(id);
            return t == null || t.CooldownSeconds <= 0.0;
        }

        /// <summary>
        /// Species DefinitionIds in the same kit whose ControlSlots (stage) &gt;= ability.Stage.
        /// </summary>
        public static string FormatUnlockSpecies(string abilityId)
        {
            DudeAbility ability = DudeAbilityRegistry.Get(abilityId);
            if (ability == null)
                return "(unknown)";

            StringBuilder sb = new StringBuilder();
            IList<DudeDefinition> all = DudeRegistry.GetAll();
            for (int i = 0; i < all.Count; i++)
            {
                DudeDefinition def = all[i];
                if (def == null || def.Type != ability.Kit)
                    continue;
                if (def.ControlSlots < ability.Stage)
                    continue;

                if (sb.Length > 0)
                    sb.Append(", ");
                sb.Append(def.Id);
            }

            if (sb.Length == 0)
                return "(none)";
            return sb.ToString();
        }

        private static void PutDefault(
            string id,
            double cd,
            double stun,
            double duration,
            double speed,
            double vsBlast,
            double healFrac,
            int radius,
            double tick,
            double hitChance,
            double gap,
            double stunMin,
            double stunMax,
            double reduce,
            double floor)
        {
            DudeAbilityTune t = new DudeAbilityTune();
            t.Id = id;
            t.CooldownSeconds = cd;
            t.StunSeconds = stun;
            t.DurationSeconds = duration;
            t.SpeedFactor = speed;
            t.DamageVsBlast = vsBlast;
            t.HealHitsFraction = healFrac;
            t.Radius = radius;
            t.TickSeconds = tick;
            t.HitChance = hitChance;
            t.GapSeconds = gap;
            t.StunMin = stunMin;
            t.StunMax = stunMax;
            t.ReduceSeconds = reduce;
            t.FloorSeconds = floor;
            m_Tunes[id] = t;
        }

        private static void WriteTune(StreamWriter writer, DudeAbilityTune t)
        {
            string id = t.Id;

            if (t.CooldownSeconds > 0.0)
                writer.WriteLine("{0}.Cooldown={1}", id, FormatDouble(t.CooldownSeconds));

            if (t.StunSeconds > 0.0)
                writer.WriteLine("{0}.StunSeconds={1}", id, FormatDouble(t.StunSeconds));
            if (t.DurationSeconds > 0.0)
                writer.WriteLine("{0}.DurationSeconds={1}", id, FormatDouble(t.DurationSeconds));
            if (t.SpeedFactor > 0.0)
                writer.WriteLine("{0}.SpeedFactor={1}", id, FormatDouble(t.SpeedFactor));
            if (t.DamageVsBlast > 0.0)
                writer.WriteLine("{0}.DamageVsBlast={1}", id, FormatDouble(t.DamageVsBlast));
            if (t.HealHitsFraction > 0.0)
                writer.WriteLine("{0}.HealHitsFraction={1}", id, FormatDouble(t.HealHitsFraction));
            if (t.Radius > 0)
                writer.WriteLine("{0}.Radius={1}", id, t.Radius);
            if (t.TickSeconds > 0.0)
                writer.WriteLine("{0}.TickSeconds={1}", id, FormatDouble(t.TickSeconds));
            if (t.HitChance > 0.0)
                writer.WriteLine("{0}.HitChance={1}", id, FormatDouble(t.HitChance));
            if (t.GapSeconds > 0.0)
                writer.WriteLine("{0}.GapSeconds={1}", id, FormatDouble(t.GapSeconds));
            if (t.StunMin > 0.0)
                writer.WriteLine("{0}.StunMin={1}", id, FormatDouble(t.StunMin));
            if (t.StunMax > 0.0)
                writer.WriteLine("{0}.StunMax={1}", id, FormatDouble(t.StunMax));
            if (t.ReduceSeconds > 0.0)
                writer.WriteLine("{0}.ReduceSeconds={1}", id, FormatDouble(t.ReduceSeconds));
            if (t.FloorSeconds > 0.0)
                writer.WriteLine("{0}.FloorSeconds={1}", id, FormatDouble(t.FloorSeconds));
        }

        private static void ApplyKeyValue(string key, string val)
        {
            if (string.Equals(key, "BlastBase", StringComparison.OrdinalIgnoreCase))
            {
                int n;
                if (int.TryParse(val, out n) && n >= 0)
                    BlastBase = n;
                return;
            }

            if (string.Equals(key, "BlastPerLevel", StringComparison.OrdinalIgnoreCase))
            {
                int n;
                if (int.TryParse(val, out n) && n >= 0)
                    BlastPerLevel = n;
                return;
            }

            int dot = key.IndexOf('.');
            if (dot <= 0 || dot >= key.Length - 1)
                return;

            string id = key.Substring(0, dot).Trim();
            string field = key.Substring(dot + 1).Trim();
            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(field))
                return;

            DudeAbilityTune t = Get(id);

            if (string.Equals(field, "Cooldown", StringComparison.OrdinalIgnoreCase)
                || string.Equals(field, "CooldownSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.CooldownSeconds = d;
            }
            else if (string.Equals(field, "StunSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.StunSeconds = d;
            }
            else if (string.Equals(field, "DurationSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.DurationSeconds = d;
            }
            else if (string.Equals(field, "SpeedFactor", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d > 0.0)
                    t.SpeedFactor = d;
            }
            else if (string.Equals(field, "DamageVsBlast", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.DamageVsBlast = d;
            }
            else if (string.Equals(field, "HealHitsFraction", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.HealHitsFraction = d;
            }
            else if (string.Equals(field, "Radius", StringComparison.OrdinalIgnoreCase))
            {
                int n;
                if (int.TryParse(val, out n) && n >= 0)
                    t.Radius = n;
            }
            else if (string.Equals(field, "TickSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.TickSeconds = d;
            }
            else if (string.Equals(field, "HitChance", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.HitChance = d;
            }
            else if (string.Equals(field, "GapSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.GapSeconds = d;
            }
            else if (string.Equals(field, "StunMin", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.StunMin = d;
            }
            else if (string.Equals(field, "StunMax", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.StunMax = d;
            }
            else if (string.Equals(field, "ReduceSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.ReduceSeconds = d;
            }
            else if (string.Equals(field, "FloorSeconds", StringComparison.OrdinalIgnoreCase))
            {
                double d;
                if (TryParseDouble(val, out d) && d >= 0.0)
                    t.FloorSeconds = d;
            }
        }

        private static bool TryParseDouble(string val, out double d)
        {
            return double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out d);
        }

        public static string FormatDouble(double d)
        {
            return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
