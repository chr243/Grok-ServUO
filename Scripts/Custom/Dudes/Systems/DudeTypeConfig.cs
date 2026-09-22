using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Live-tunable per-type balance table, persisted to Data/DudeTypes.cfg.
    /// DudeTypeProfiles registers code defaults, then this overlay applies any file values.
    /// Adding or retuning a type needs no recompile — edit the cfg and [DudeTypesReload.
    ///
    /// Format (ini-ish sections, one per type id):
    ///     [fire]
    ///     Name=Fire
    ///     ShortsHue=0x21
    ///     BaseStr=45
    ///     BaseDex=40
    ///     ...
    /// </summary>
    public static class DudeTypeConfig
    {
        public static readonly string FilePath = Path.Combine("Data", "DudeTypes.cfg");

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

            // Defaults are registered by DudeTypeProfiles; only overlay the file.
            DudeTypeProfiles.EnsureInitialized();

            if (!File.Exists(FilePath))
            {
                Save(); // write a template so the table is discoverable/editable
                return;
            }

            ApplyFile(FilePath);
        }

        public static void Reload()
        {
            m_Loaded = false;
            DudeTypeProfiles.ResetToDefaults();
            Load();
        }

        private static void ApplyFile(string path)
        {
            DudeTypeProfile current = null;

            try
            {
                string[] lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (line == null)
                        continue;

                    line = line.Trim();
                    if (line.Length == 0 || line[0] == '#' || line[0] == ';')
                        continue;

                    if (line[0] == '[')
                    {
                        int close = line.IndexOf(']');
                        if (close > 1)
                        {
                            string id = line.Substring(1, close - 1).Trim();
                            current = DudeTypeProfiles.GetById(id);
                        }
                        continue;
                    }

                    if (current == null)
                        continue;

                    int eq = line.IndexOf('=');
                    if (eq <= 0)
                        continue;

                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    ApplyField(current, key, val);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("DudeTypeConfig: failed to load {0}: {1}", path, ex.Message);
            }
        }

        private static void ApplyField(DudeTypeProfile p, string key, string val)
        {
            double d;

            if (string.Equals(key, "Name", StringComparison.OrdinalIgnoreCase))
                p.Name = val;
            else if (string.Equals(key, "ShortsHue", StringComparison.OrdinalIgnoreCase))
                p.ShortsHue = ParseInt(val);
            else if (string.Equals(key, "SkinHueMode", StringComparison.OrdinalIgnoreCase))
                p.SkinHueMode = val;
            else if (string.Equals(key, "SoundId", StringComparison.OrdinalIgnoreCase))
                p.SoundId = ParseInt(val);
            else if (string.Equals(key, "Vfx", StringComparison.OrdinalIgnoreCase))
                p.Vfx = ParseVfx(val);
            else if (string.Equals(key, "SpawnWeight", StringComparison.OrdinalIgnoreCase))
                p.SpawnWeight = ParseInt(val);
            else if (string.Equals(key, "BaseStr", StringComparison.OrdinalIgnoreCase))
                p.BaseStr = ParseInt(val);
            else if (string.Equals(key, "BaseDex", StringComparison.OrdinalIgnoreCase))
                p.BaseDex = ParseInt(val);
            else if (string.Equals(key, "BaseInt", StringComparison.OrdinalIgnoreCase))
                p.BaseInt = ParseInt(val);
            else if (string.Equals(key, "BaseHits", StringComparison.OrdinalIgnoreCase))
                p.BaseHits = ParseInt(val);
            else if (string.Equals(key, "BaseMinDamage", StringComparison.OrdinalIgnoreCase))
                p.BaseMinDamage = ParseInt(val);
            else if (string.Equals(key, "BaseMaxDamage", StringComparison.OrdinalIgnoreCase))
                p.BaseMaxDamage = ParseInt(val);
            else if (string.Equals(key, "BaseVirtualArmor", StringComparison.OrdinalIgnoreCase))
                p.BaseVirtualArmor = ParseInt(val);
            else if (string.Equals(key, "GainStr", StringComparison.OrdinalIgnoreCase))
                p.GainStr = ParseInt(val);
            else if (string.Equals(key, "GainDex", StringComparison.OrdinalIgnoreCase))
                p.GainDex = ParseInt(val);
            else if (string.Equals(key, "GainInt", StringComparison.OrdinalIgnoreCase))
                p.GainInt = ParseInt(val);
            else if (string.Equals(key, "HitsGainMultiplier", StringComparison.OrdinalIgnoreCase))
            {
                if (TryParseDouble(val, out d) && d >= 0.0)
                    p.HitsGainMultiplier = d;
            }
            else if (string.Equals(key, "ArmorGainAmount", StringComparison.OrdinalIgnoreCase))
                p.ArmorGainAmount = ParseInt(val);
            else if (string.Equals(key, "ArmorGainInterval", StringComparison.OrdinalIgnoreCase))
                p.ArmorGainInterval = ParseInt(val);
            else if (string.Equals(key, "ManaPerInt", StringComparison.OrdinalIgnoreCase))
                p.ManaPerInt = ParseInt(val);
        }

        public static bool Save()
        {
            try
            {
                string dir = Path.GetDirectoryName(FilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("# Dude per-type balance table. Edit then run [DudeTypesReload.");
                sb.AppendLine("# One [section] per type id. Values here override code defaults.");
                sb.AppendLine("# HitsGainMultiplier scales the shared hits band; ArmorGainInterval=0 disables armor gain.");
                sb.AppendLine();

                IList<DudeTypeProfile> all = DudeTypeProfiles.GetAll();
                for (int i = 0; i < all.Count; i++)
                {
                    DudeTypeProfile p = all[i];
                    sb.AppendLine("[" + p.Id + "]");
                    sb.AppendLine("Name=" + p.Name);
                    sb.AppendLine("ShortsHue=0x" + p.ShortsHue.ToString("X"));
                    sb.AppendLine("SkinHueMode=" + p.SkinHueMode);
                    sb.AppendLine("SoundId=0x" + p.SoundId.ToString("X"));
                    sb.AppendLine("Vfx=" + p.Vfx);
                    sb.AppendLine("SpawnWeight=" + p.SpawnWeight);
                    sb.AppendLine("BaseStr=" + p.BaseStr);
                    sb.AppendLine("BaseDex=" + p.BaseDex);
                    sb.AppendLine("BaseInt=" + p.BaseInt);
                    sb.AppendLine("BaseHits=" + p.BaseHits);
                    sb.AppendLine("BaseMinDamage=" + p.BaseMinDamage);
                    sb.AppendLine("BaseMaxDamage=" + p.BaseMaxDamage);
                    sb.AppendLine("BaseVirtualArmor=" + p.BaseVirtualArmor);
                    sb.AppendLine("GainStr=" + p.GainStr);
                    sb.AppendLine("GainDex=" + p.GainDex);
                    sb.AppendLine("GainInt=" + p.GainInt);
                    sb.AppendLine("HitsGainMultiplier=" + FormatDouble(p.HitsGainMultiplier));
                    sb.AppendLine("ArmorGainAmount=" + p.ArmorGainAmount);
                    sb.AppendLine("ArmorGainInterval=" + p.ArmorGainInterval);
                    sb.AppendLine("ManaPerInt=" + p.ManaPerInt);
                    sb.AppendLine();
                }

                File.WriteAllText(FilePath, sb.ToString());
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("DudeTypeConfig: failed to save {0}: {1}", FilePath, ex.Message);
                return false;
            }
        }

        private static int ParseInt(string val)
        {
            if (string.IsNullOrEmpty(val))
                return 0;

            val = val.Trim();
            if (val.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                int hex;
                if (int.TryParse(val.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out hex))
                    return hex;
                return 0;
            }

            int n;
            if (int.TryParse(val, out n))
                return n;
            return 0;
        }

        private static DudeVfx ParseVfx(string val)
        {
            try
            {
                return (DudeVfx)Enum.Parse(typeof(DudeVfx), val, true);
            }
            catch
            {
                return DudeVfx.Fire;
            }
        }

        private static bool TryParseDouble(string val, out double d)
        {
            return double.TryParse(val, NumberStyles.Float, CultureInfo.InvariantCulture, out d);
        }

        private static string FormatDouble(double d)
        {
            return d.ToString("0.###", CultureInfo.InvariantCulture);
        }
    }
}