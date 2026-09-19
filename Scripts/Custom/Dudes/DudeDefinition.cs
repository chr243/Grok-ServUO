namespace Server.Custom.Dudes
{
    /// <summary>
    /// Immutable template for a Dude species. Register new species via DudeRegistry.
    /// Body is always human male (0x190). Hue is the type color used on shorts.
    /// </summary>
    public sealed class DudeDefinition
    {
        private readonly string m_Id;
        private readonly string m_Name;
        private readonly DudeType m_Type;
        private readonly int m_Body;
        private readonly int m_Hue;
        private readonly int m_BaseSoundID;
        private readonly int m_Str;
        private readonly int m_Dex;
        private readonly int m_Int;
        private readonly int m_Hits;
        private readonly int m_MinDamage;
        private readonly int m_MaxDamage;
        private readonly int m_VirtualArmor;
        private readonly string m_AbilityId;
        private readonly int m_ControlSlots;

        public DudeDefinition(
            string id,
            string name,
            DudeType type,
            int body,
            int hue,
            int baseSoundID,
            int str,
            int dex,
            int intel,
            int hits,
            int minDamage,
            int maxDamage,
            int virtualArmor,
            string abilityId,
            int controlSlots)
        {
            m_Id = id;
            m_Name = name;
            m_Type = type;
            m_Body = body;
            m_Hue = hue;
            m_BaseSoundID = baseSoundID;
            m_Str = str;
            m_Dex = dex;
            m_Int = intel;
            m_Hits = hits;
            m_MinDamage = minDamage;
            m_MaxDamage = maxDamage;
            m_VirtualArmor = virtualArmor;
            m_AbilityId = abilityId;
            m_ControlSlots = controlSlots;
        }

        public string Id { get { return m_Id; } }
        public string Name { get { return m_Name; } }
        public DudeType Type { get { return m_Type; } }
        public int Body { get { return m_Body; } }
        public int Hue { get { return m_Hue; } }
        public int BaseSoundID { get { return m_BaseSoundID; } }
        public int Str { get { return m_Str; } }
        public int Dex { get { return m_Dex; } }
        public int Int { get { return m_Int; } }
        public int Hits { get { return m_Hits; } }
        public int MinDamage { get { return m_MinDamage; } }
        public int MaxDamage { get { return m_MaxDamage; } }
        public int VirtualArmor { get { return m_VirtualArmor; } }
        public string AbilityId { get { return m_AbilityId; } }
        public int ControlSlots { get { return m_ControlSlots; } }
    }
}
