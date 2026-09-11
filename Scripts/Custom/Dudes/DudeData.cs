using System;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Persistent Dude state. Authoritative copy lives on DudeBall (not the world creature).
    /// </summary>
    public sealed class DudeData
    {
        private string m_DefinitionId;
        private string m_CustomName;
        private DudeType m_Type;
        private int m_Level;
        private int m_CurrentEXP;
        private int m_EXPToNext;
        private int m_Str;
        private int m_Dex;
        private int m_Int;
        private int m_HitsMax;
        private int m_Hits;
        private int m_MinDamage;
        private int m_MaxDamage;
        private int m_VirtualArmor;
        private string m_AbilityId;
        private bool m_IsFainted;
        private Mobile m_Catcher;

        public DudeData()
        {
            m_Level = 1;
            m_CurrentEXP = 0;
            m_EXPToNext = DudeExperience.GetExpRequiredForLevel(1);
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
            set { m_Type = value; }
        }

        public int Level
        {
            get { return m_Level; }
            set { m_Level = value; }
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

        public int Str
        {
            get { return m_Str; }
            set { m_Str = value; }
        }

        public int Dex
        {
            get { return m_Dex; }
            set { m_Dex = value; }
        }

        public int Int
        {
            get { return m_Int; }
            set { m_Int = value; }
        }

        public int HitsMax
        {
            get { return m_HitsMax; }
            set { m_HitsMax = value; }
        }

        public int Hits
        {
            get { return m_Hits; }
            set { m_Hits = value; }
        }

        public int MinDamage
        {
            get { return m_MinDamage; }
            set { m_MinDamage = value; }
        }

        public int MaxDamage
        {
            get { return m_MaxDamage; }
            set { m_MaxDamage = value; }
        }

        public int VirtualArmor
        {
            get { return m_VirtualArmor; }
            set { m_VirtualArmor = value; }
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

        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(m_CustomName))
                    return m_CustomName;

                DudeDefinition def = DudeRegistry.Get(m_DefinitionId);
                if (def != null)
                    return def.Name;

                return "Dude";
            }
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
            data.m_Str = def.Str;
            data.m_Dex = def.Dex;
            data.m_Int = def.Int;
            data.m_HitsMax = def.Hits;
            data.m_Hits = def.Hits;
            data.m_MinDamage = def.MinDamage;
            data.m_MaxDamage = def.MaxDamage;
            data.m_VirtualArmor = def.VirtualArmor;
            data.m_AbilityId = def.AbilityId;
            data.m_IsFainted = false;
            data.m_Catcher = catcher;
            return data;
        }

        public void Serialize(GenericWriter writer)
        {
            writer.Write((int)0); // version

            writer.Write(m_DefinitionId);
            writer.Write(m_CustomName);
            writer.Write((int)m_Type);
            writer.Write(m_Level);
            writer.Write(m_CurrentEXP);
            writer.Write(m_EXPToNext);
            writer.Write(m_Str);
            writer.Write(m_Dex);
            writer.Write(m_Int);
            writer.Write(m_HitsMax);
            writer.Write(m_Hits);
            writer.Write(m_MinDamage);
            writer.Write(m_MaxDamage);
            writer.Write(m_VirtualArmor);
            writer.Write(m_AbilityId);
            writer.Write(m_IsFainted);
            writer.Write(m_Catcher);
        }

        public void Deserialize(GenericReader reader)
        {
            int version = reader.ReadInt();

            switch (version)
            {
                case 0:
                    {
                        m_DefinitionId = reader.ReadString();
                        m_CustomName = reader.ReadString();
                        m_Type = (DudeType)reader.ReadInt();
                        m_Level = reader.ReadInt();
                        m_CurrentEXP = reader.ReadInt();
                        m_EXPToNext = reader.ReadInt();
                        m_Str = reader.ReadInt();
                        m_Dex = reader.ReadInt();
                        m_Int = reader.ReadInt();
                        m_HitsMax = reader.ReadInt();
                        m_Hits = reader.ReadInt();
                        m_MinDamage = reader.ReadInt();
                        m_MaxDamage = reader.ReadInt();
                        m_VirtualArmor = reader.ReadInt();
                        m_AbilityId = reader.ReadString();
                        m_IsFainted = reader.ReadBool();
                        m_Catcher = reader.ReadMobile();
                        break;
                    }
            }
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
            copy.m_Str = m_Str;
            copy.m_Dex = m_Dex;
            copy.m_Int = m_Int;
            copy.m_HitsMax = m_HitsMax;
            copy.m_Hits = m_Hits;
            copy.m_MinDamage = m_MinDamage;
            copy.m_MaxDamage = m_MaxDamage;
            copy.m_VirtualArmor = m_VirtualArmor;
            copy.m_AbilityId = m_AbilityId;
            copy.m_IsFainted = m_IsFainted;
            copy.m_Catcher = m_Catcher;
            return copy;
        }
    }
}
