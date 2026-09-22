using System;
using System.Collections.Generic;
using Server;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Persistent Dude state. Authoritative copy lives on DudeBall (not the world creature).
    ///
    /// STORAGE MODEL B1: only IDENTITY + PROGRESS are persisted
    ///     (Type, Level, Stage, the three IV rolls, combat skills, current HP).
    /// All combat stats (Str/Dex/Int/HitsMax/Damage/Armor/Mana) are DERIVED by
    /// <see cref="DudeFormulas.Derive"/> and recomputed on every event that can change
    /// them (create, level-up, ascension, skill change, load). They are never serialized,
    /// so a recalled Dude can never disagree with its saved self.
    /// </summary>
    public sealed class DudeData
    {
        private string m_DefinitionId;
        private string m_CustomName;
        private DudeType m_Type;
        private int m_Level;
        private int m_CurrentEXP;
        private int m_EXPToNext;

        // --- Derived (recomputed, not persisted) ---
        private int m_Str;
        private int m_Dex;
        private int m_Int;
        private int m_HitsMax;
        private int m_MinDamage;
        private int m_MaxDamage;
        private int m_VirtualArmor;
        private int m_ManaMax;

        // --- IVs: the only individual variance. Rolled once, never re-rolled. ---
        private int m_StrMod;
        private int m_DexMod;
        private int m_IntMod;

        // --- Persisted: current HP only (HitsMax is derived) ---
        private int m_Hits;

        private string m_AbilityId;
        private bool m_IsFainted;
        private Mobile m_Catcher;
        private double m_GatherSkill;
        private int m_EvolutionStage;
        private string m_UnlockedAbilities;
        private double m_Wrestling;
        private double m_Tactics;
        private double m_Anatomy;
        private double m_MagicResist;
        private int m_SkinHue;

        public DudeData()
        {
            m_Level = 1;
            m_CurrentEXP = 0;
            m_EXPToNext = DudeExperience.GetExpRequiredForLevel(1);
            m_GatherSkill = Server.Custom.Dudes.Jobs.DudeJobConfig.BaseGatherSkill;
            m_EvolutionStage = 1;
            m_UnlockedAbilities = null;
        }

        public string DefinitionId
        {
            get { return m_DefinitionId; }
            set { m_DefinitionId = value; }
        }

        public string CustomName
        {
            get { return m_CustomName; }
            set { m_CustomName = value; }
        }

        public DudeType Type
        {
            get { return m_Type; }
            set
            {
                if (m_Type == value)
                    return;
                m_Type = value;
                RecomputeStats();
            }
        }

        public int Level
        {
            get { return m_Level; }
            set
            {
                int v = value < 1 ? 1 : value;
                if (m_Level == v)
                    return;
                m_Level = v;
                RecomputeStats();
            }
        }

        public int CurrentEXP
        {
            get { return m_CurrentEXP; }
            set { m_CurrentEXP = value; }
        }

        public int EXPToNext
        {
            get { return m_EXPToNext; }
            set { m_EXPToNext = value; }
        }

        // --- Derived, read-only from outside ---

        public int Str { get { return m_Str; } }
        public int Dex { get { return m_Dex; } }
        public int Int { get { return m_Int; } }
        public int HitsMax { get { return m_HitsMax; } }
        public int MinDamage { get { return m_MinDamage; } }
        public int MaxDamage { get { return m_MaxDamage; } }
        public int VirtualArmor { get { return m_VirtualArmor; } }
        public int ManaMax { get { return m_ManaMax; } }

        /// <summary>One-time IV-style Str roll at create/catch. Never re-rolled on level-up.</summary>
        public int StrMod
        {
            get { return m_StrMod; }
            set { m_StrMod = value; RecomputeStats(); }
        }

        public int DexMod
        {
            get { return m_DexMod; }
            set { m_DexMod = value; RecomputeStats(); }
        }

        public int IntMod
        {
            get { return m_IntMod; }
            set { m_IntMod = value; RecomputeStats(); }
        }

        /// <summary>Current hit points. The only HP value that is persisted.</summary>
        public int Hits
        {
            get { return m_Hits; }
            set
            {
                int v = value < 0 ? 0 : value;
                if (m_HitsMax > 0 && v > m_HitsMax)
                    v = m_HitsMax;
                m_Hits = v;
            }
        }

        public string AbilityId
        {
            get { return m_AbilityId; }
            set { m_AbilityId = value; }
        }

        public bool IsFainted
        {
            get { return m_IsFainted; }
            set { m_IsFainted = value; }
        }

        public Mobile Catcher
        {
            get { return m_Catcher; }
            set { m_Catcher = value; }
        }

        /// <summary>Persistent gathering skill (jobs), independent of combat level.</summary>
        public double GatherSkill
        {
            get { return m_GatherSkill; }
            set
            {
                double max = Server.Custom.Dudes.Jobs.DudeJobConfig.MaxGatherSkill;
                if (value < 0.0)
                    value = 0.0;
                else if (value > max)
                    value = max;
                m_GatherSkill = value;
            }
        }

        /// <summary>1 = base form, 2 = first ascension, 3 = final ascension.</summary>
        public int EvolutionStage
        {
            get { return m_EvolutionStage < 1 ? 1 : m_EvolutionStage; }
            set
            {
                int v = value < 1 ? 1 : value;
                if (m_EvolutionStage == v)
                    return;
                m_EvolutionStage = v;
                RecomputeStats();
            }
        }

        /// <summary>Gear slots unlocked at this Dude's ascension stage (2 / 3 / 4).</summary>
        public int GetGearSlots()
        {
            return DudeStage.GearSlots(EvolutionStage);
        }

        /// <summary>Comma-separated unlocked ability ids (always includes primary).</summary>
        public string UnlockedAbilities
        {
            get { return m_UnlockedAbilities; }
            set { m_UnlockedAbilities = value; }
        }

        /// <summary>Persistent combat skill (stage-scaled cap: 100/110/120). Wild catch rolls 40-60.</summary>
        public double Wrestling
        {
            get { return m_Wrestling; }
            set
            {
                m_Wrestling = DudeCombatSkills.Clamp(value, this);
                RecomputeStats();
            }
        }

        public double Tactics
        {
            get { return m_Tactics; }
            set
            {
                m_Tactics = DudeCombatSkills.Clamp(value, this);
                RecomputeStats();
            }
        }

        public double Anatomy
        {
            get { return m_Anatomy; }
            set
            {
                m_Anatomy = DudeCombatSkills.Clamp(value, this);
                RecomputeStats();
            }
        }

        public double MagicResist
        {
            get { return m_MagicResist; }
            set
            {
                m_MagicResist = DudeCombatSkills.Clamp(value, this);
                RecomputeStats();
            }
        }

        /// <summary>Persistent human skin hue. 0 = unset (roll on apply).</summary>
        public int SkinHue
        {
            get { return m_SkinHue; }
            set { m_SkinHue = value; }
        }

        /// <summary>
        /// SINGLE WRITE PATH for derived stats. Called whenever identity or progress changes:
        /// creation, catch, level-up, ascension, skill change, deserialize.
        /// Never call from render/tooltip paths.
        /// </summary>
        public void RecomputeStats()
        {
            DudeStats s = DudeFormulas.Derive(
                m_Type, m_Level, EvolutionStage,
                m_StrMod, m_DexMod, m_IntMod,
                m_Wrestling, m_Tactics, m_Anatomy, m_MagicResist);

            m_Str = s.Str;
            m_Dex = s.Dex;
            m_Int = s.Int;
            m_HitsMax = s.HitsMax;
            m_MinDamage = s.MinDamage;
            m_MaxDamage = s.MaxDamage;
            m_VirtualArmor = s.VirtualArmor;
            m_ManaMax = s.ManaMax;

            // Refresh skill caches with the stage-clamped values the formula returned.
            m_Wrestling = s.Wrestling;
            m_Tactics = s.Tactics;
            m_Anatomy = s.Anatomy;
            m_MagicResist = s.MagicResist;

            if (m_Hits > m_HitsMax)
                m_Hits = m_HitsMax;
        }

        public string DisplayName
        {
            get
            {
                DudeDefinition def = DudeRegistry.Get(m_DefinitionId);
                string species = def != null ? def.Name : null;
                if (species == null)
                {
                    DudeTypeProfile profile = DudeTypeProfiles.Get(m_Type);
                    if (profile != null)
                        species = profile.Name;
                }

                string raw;
                if (!string.IsNullOrEmpty(m_CustomName))
                    raw = m_CustomName;
                else if (!string.IsNullOrEmpty(species))
                    raw = species;
                else
                    return "Dude";

                if (HasDudeSuffix(raw))
                    return raw;

                // Species-default name (or CustomName equal to species) -> "{Name} Dude"
                if (!string.IsNullOrEmpty(species)
                    && string.Equals(raw, species, StringComparison.OrdinalIgnoreCase))
                    return species + " Dude";

                return raw;
            }
        }

        private static bool HasDudeSuffix(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            return name.EndsWith(" Dude", StringComparison.OrdinalIgnoreCase)
                || string.Equals(name, "Dude", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Ability ids available for combat. Temporary: empty until gear cache grants them.
        /// Ascension no longer unlocks kit abilities for combat.
        /// </summary>
        public List<string> GetUnlockedAbilityIds()
        {
            // Next step: return ids from equipped gear cache when present.
            return new List<string>();
        }

        /// <summary>Legacy storage helper for persisted unlock strings (not used by combat).</summary>
        private List<string> ParseStoredUnlockedAbilityIds()
        {
            List<string> list = new List<string>();

            if (!string.IsNullOrEmpty(m_UnlockedAbilities))
            {
                string[] parts = m_UnlockedAbilities.Split(',');
                for (int i = 0; i < parts.Length; i++)
                {
                    string id = parts[i] != null ? parts[i].Trim() : null;
                    if (string.IsNullOrEmpty(id))
                        continue;

                    bool found = false;
                    for (int j = 0; j < list.Count; j++)
                    {
                        if (string.Equals(list[j], id, StringComparison.OrdinalIgnoreCase))
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                        list.Add(id);
                }
            }

            if (!string.IsNullOrEmpty(m_AbilityId))
            {
                bool hasPrimary = false;
                for (int i = 0; i < list.Count; i++)
                {
                    if (string.Equals(list[i], m_AbilityId, StringComparison.OrdinalIgnoreCase))
                    {
                        hasPrimary = true;
                        break;
                    }
                }

                if (!hasPrimary)
                    list.Insert(0, m_AbilityId);
            }

            return list;
        }

        public void UnlockAbility(string abilityId)
        {
            if (string.IsNullOrEmpty(abilityId))
                return;

            List<string> list = ParseStoredUnlockedAbilityIds();
            for (int i = 0; i < list.Count; i++)
            {
                if (string.Equals(list[i], abilityId, StringComparison.OrdinalIgnoreCase))
                    return;
            }

            list.Add(abilityId);
            m_UnlockedAbilities = string.Join(",", list.ToArray());
        }

        public static DudeData FromDefinition(DudeDefinition def, Mobile catcher)
        {
            if (def == null)
                return null;

            DudeData data = new DudeData();
            data.m_DefinitionId = def.Id;
            data.m_CustomName = def.Name;
            data.m_Type = def.Type;
            data.m_Level = 1;
            data.m_CurrentEXP = 0;
            data.m_EXPToNext = DudeExperience.GetExpRequiredForLevel(1);

            // One-time IV roll; the only individual variance. Baked into derived stats.
            data.m_StrMod = Utility.RandomMinMax(-4, 4);
            data.m_DexMod = Utility.RandomMinMax(-4, 4);
            data.m_IntMod = Utility.RandomMinMax(-4, 4);

            data.m_AbilityId = def.AbilityId;
            data.m_UnlockedAbilities = def.AbilityId;
            data.m_EvolutionStage = 1;
            data.m_IsFainted = false;
            data.m_Catcher = catcher;
            data.m_GatherSkill = Server.Custom.Dudes.Jobs.DudeJobConfig.BaseGatherSkill;

            data.m_Wrestling = DudeCombatSkills.Roll();
            data.m_Tactics = DudeCombatSkills.Roll();
            data.m_Anatomy = DudeCombatSkills.Roll();
            data.m_MagicResist = DudeCombatSkills.Roll();

            // Derive everything from (Type, Level 1, Stage 1, IVs, skills).
            data.RecomputeStats();
            data.m_Hits = data.m_HitsMax;
            return data;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)6); // version - derived stats are no longer persisted

            writer.Write(m_DefinitionId);
            writer.Write(m_CustomName);
            writer.Write((int)m_Type);
            writer.Write(m_Level);
            writer.Write(m_CurrentEXP);
            writer.Write(m_EXPToNext);
            writer.Write(m_Hits);      // current HP only
            writer.Write(m_AbilityId);
            writer.Write(m_IsFainted);
            writer.Write(m_Catcher);
            writer.Write(m_GatherSkill);
            writer.Write(m_EvolutionStage);
            writer.Write(m_UnlockedAbilities);
            writer.Write(m_StrMod);
            writer.Write(m_DexMod);
            writer.Write(m_IntMod);
            writer.Write(m_Wrestling);
            writer.Write(m_Tactics);
            writer.Write(m_Anatomy);
            writer.Write(m_MagicResist);
            writer.Write(m_SkinHue);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            m_DefinitionId = reader.ReadString();
            m_CustomName = reader.ReadString();
            m_Type = (DudeType)reader.ReadInt();
            m_Level = reader.ReadInt();
            m_CurrentEXP = reader.ReadInt();
            m_EXPToNext = reader.ReadInt();

            if (version >= 6)
            {
                m_Hits = reader.ReadInt();
            }
            else
            {
                // Legacy layout (v5 and below) persisted derived stats.
                // Read and discard them; RecomputeStats rebuilds them below.
                // Old field order: Str, Dex, Int, HitsMax, Hits, MinDamage, MaxDamage, VirtualArmor.
                reader.ReadInt();      // old m_Str
                reader.ReadInt();      // old m_Dex
                reader.ReadInt();      // old m_Int
                reader.ReadInt();      // old m_HitsMax
                m_Hits = reader.ReadInt(); // old m_Hits (authoritative current HP)
                reader.ReadInt();      // old m_MinDamage
                reader.ReadInt();      // old m_MaxDamage
                reader.ReadInt();      // old m_VirtualArmor
            }

            m_AbilityId = reader.ReadString();
            m_IsFainted = reader.ReadBool();
            m_Catcher = reader.ReadMobile();

            if (version >= 1)
            {
                m_GatherSkill = reader.ReadDouble();
            }
            else
            {
                // Migrate old level-tied job skill so existing Dudes keep their harvest tier.
                m_GatherSkill = DudeExperience.GetSkillCapForLevel(m_Level > 0 ? m_Level : 1);
            }

            if (version >= 2)
            {
                m_EvolutionStage = reader.ReadInt();
                m_UnlockedAbilities = reader.ReadString();
            }
            else
            {
                m_EvolutionStage = 1;
                m_UnlockedAbilities = m_AbilityId;
            }

            if (version >= 3)
            {
                m_StrMod = reader.ReadInt();
                m_DexMod = reader.ReadInt();
                m_IntMod = reader.ReadInt();
            }
            else
            {
                m_StrMod = 0;
                m_DexMod = 0;
                m_IntMod = 0;
            }

            if (version >= 4)
            {
                m_Wrestling = reader.ReadDouble();
                m_Tactics = reader.ReadDouble();
                m_Anatomy = reader.ReadDouble();
                m_MagicResist = reader.ReadDouble();
            }
            else
            {
                m_Wrestling = DudeCombatSkills.Roll();
                m_Tactics = DudeCombatSkills.Roll();
                m_Anatomy = DudeCombatSkills.Roll();
                m_MagicResist = DudeCombatSkills.Roll();
            }

            if (version >= 5)
                m_SkinHue = reader.ReadInt();
            else
                m_SkinHue = 0;

            if (m_EvolutionStage < 1)
                m_EvolutionStage = 1;

            m_Wrestling = DudeCombatSkills.Clamp(m_Wrestling, m_EvolutionStage);
            m_Tactics = DudeCombatSkills.Clamp(m_Tactics, m_EvolutionStage);
            m_Anatomy = DudeCombatSkills.Clamp(m_Anatomy, m_EvolutionStage);
            m_MagicResist = DudeCombatSkills.Clamp(m_MagicResist, m_EvolutionStage);

            if (string.IsNullOrEmpty(m_UnlockedAbilities) && !string.IsNullOrEmpty(m_AbilityId))
                m_UnlockedAbilities = m_AbilityId;

            double max = Server.Custom.Dudes.Jobs.DudeJobConfig.MaxGatherSkill;
            if (m_GatherSkill < 0.0)
                m_GatherSkill = 0.0;
            else if (m_GatherSkill > max)
                m_GatherSkill = max;

            // Rebuild all derived stats from identity + progress.
            RecomputeStats();
        }

        public DudeData Clone()
        {
            DudeData copy = new DudeData();
            copy.m_DefinitionId = m_DefinitionId;
            copy.m_CustomName = m_CustomName;
            copy.m_Type = m_Type;
            copy.m_Level = m_Level;
            copy.m_CurrentEXP = m_CurrentEXP;
            copy.m_EXPToNext = m_EXPToNext;
            copy.m_StrMod = m_StrMod;
            copy.m_DexMod = m_DexMod;
            copy.m_IntMod = m_IntMod;
            copy.m_Hits = m_Hits;
            copy.m_AbilityId = m_AbilityId;
            copy.m_IsFainted = m_IsFainted;
            copy.m_Catcher = m_Catcher;
            copy.m_GatherSkill = m_GatherSkill;
            copy.m_EvolutionStage = m_EvolutionStage;
            copy.m_UnlockedAbilities = m_UnlockedAbilities;
            copy.m_Wrestling = m_Wrestling;
            copy.m_Tactics = m_Tactics;
            copy.m_Anatomy = m_Anatomy;
            copy.m_MagicResist = m_MagicResist;
            copy.m_SkinHue = m_SkinHue;
            copy.RecomputeStats();
            return copy;
        }
    }
}