using System;
using System.IO;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Global live-tunable Dude scaling. Persisted to Data/DudeScaling.cfg.
    /// Edited in-game via [DudeScale — no restart required.
    /// </summary>
    public static class DudeScalingConfig
    {
        public static readonly string FilePath = Path.Combine("Data", "DudeScaling.cfg");

        /// <summary>Multiplies GetExpRequiredForLevel curve (higher = slower leveling).</summary>
        public static double ExpScale = 1.0;

        /// <summary>Hits added on each level-up for levels 2–10.</summary>
        public static int HitsGainL2to10 = 17;

        /// <summary>Hits added on each level-up for levels 11–20.</summary>
        public static int HitsGainL11to20 = 30;

        /// <summary>Hits added on each level-up for levels 21–30.</summary>
        public static int HitsGainL21to30 = 50;

        /// <summary>Melee MinDamage and MaxDamage gained per level-up.</summary>
        public static int MeleeDamagePerLevel = 2;

        /// <summary>Multiplies ability / blast damage formulas.</summary>
        public static double AbilityDamageMultiplier = 1.0;

        private static bool m_Loaded;

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

            // Defaults first, then overlay file if present.
            ExpScale = 1.0;
            HitsGainL2to10 = 17;
            HitsGainL11to20 = 30;
            HitsGainL21to30 = 50;
            MeleeDamagePerLevel = 2;
            AbilityDamageMultiplier = 1.0;

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

                    if (string.Equals(key, "ExpScale", StringComparison.OrdinalIgnoreCase))
                    {
                        double d;
                        if (TryParseDouble(val, out d) && d > 0.0)
                            ExpScale = d;
                    }
                    else if (string.Equals(key, "HitsGainL2to10", StringComparison.OrdinalIgnoreCase))
                    {
                        int n;
                        if (int.TryParse(val, out n) && n >= 0)
                            HitsGainL2to10 = n;
                    }
                    else if (string.Equals(key, "HitsGainL11to20", StringComparison.OrdinalIgnoreCase))
                    {
                        int n;
                        if (int.TryParse(val, out n) && n >= 0)
                            HitsGainL11to20 = n;
                    }
                    else if (string.Equals(key, "HitsGainL21to30", StringComparison.OrdinalIgnoreCase))
                    {
                        int n;
                        if (int.TryParse(val, out n) && n >= 0)
                            HitsGainL21to30 = n;
                    }
                    else if (string.Equals(key, "MeleeDamagePerLevel", StringComparison.OrdinalIgnoreCase))
                    {
                        int n;
                        if (int.TryParse(val, out n) && n >= 0)
                            MeleeDamagePerLevel = n;
                    }
                    else if (string.Equals(key, "AbilityDamageMultiplier", StringComparison.OrdinalIgnoreCase))
                    {
                        double d;
                        if (TryParseDouble(val, out d) && d > 0.0)
                            AbilityDamageMultiplier = d;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DudeScalingConfig: failed to load {0}: {1}", FilePath, ex.Message);
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
                    writer.WriteLine("# Dude global scaling — edited via [DudeScale");
                    writer.WriteLine("# Existing Dudes keep stored Hits; new LevelUps / EXP curve use these values.");
                    writer.WriteLine("ExpScale={0}", FormatDouble(ExpScale));
                    writer.WriteLine("HitsGainL2to10={0}", HitsGainL2to10);
                    writer.WriteLine("HitsGainL11to20={0}", HitsGainL11to20);
                    writer.WriteLine("HitsGainL21to30={0}", HitsGainL21to30);
                    writer.WriteLine("MeleeDamagePerLevel={0}", MeleeDamagePerLevel);
                    writer.WriteLine("AbilityDamageMultiplier={0}", FormatDouble(AbilityDamageMultiplier));
                }

                m_Loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DudeScalingConfig: failed to save {0}: {1}", FilePath, ex.Message);
                return false;
            }
        }

        public static void ResetDefaults()
        {
            ExpScale = 1.0;
            HitsGainL2to10 = 17;
            HitsGainL11to20 = 30;
            HitsGainL21to30 = 50;
            MeleeDamagePerLevel = 2;
            AbilityDamageMultiplier = 1.0;
        }

        private static bool TryParseDouble(string val, out double d)
        {
            return double.TryParse(val, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out d);
        }

        private static string FormatDouble(double d)
        {
            return d.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
        }
    }
}
