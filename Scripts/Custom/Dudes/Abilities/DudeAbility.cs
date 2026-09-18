using System;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Extensible Dude ability. Name, cooldown, optional mana cost, kit/stage, Execute().
    /// Cooldown reads live from DudeAbilityConfig when configured.
    /// </summary>
    public abstract class DudeAbility
    {
        private readonly string m_Id;
        private readonly string m_Name;
        private readonly TimeSpan m_Cooldown;
        private readonly int m_ManaCost;
        private readonly int m_Stage;
        private readonly DudeType m_Kit;

        protected DudeAbility(string id, string name, TimeSpan cooldown, int manaCost, int stage, DudeType kit)
        {
            m_Id = id;
            m_Name = name;
            m_Cooldown = cooldown;
            m_ManaCost = manaCost;
            m_Stage = stage;
            m_Kit = kit;
        }

        public string Id { get { return m_Id; } }
        public string Name { get { return m_Name; } }

        /// <summary>
        /// Live cooldown from DudeAbilityConfig when CooldownSeconds &gt; 0; else ctor fallback.
        /// </summary>
        public TimeSpan Cooldown
        {
            get
            {
                DudeAbilityConfig.EnsureLoaded();
                TimeSpan live = DudeAbilityConfig.GetCooldown(m_Id);
                if (live > TimeSpan.Zero)
                    return live;
                return m_Cooldown;
            }
        }

        public int ManaCost { get { return m_ManaCost; } }
        public int Stage { get { return m_Stage; } }
        public DudeType Kit { get { return m_Kit; } }

        public virtual bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted || target == null || target.Deleted || !target.Alive)
                return false;

            if (dude.IsWild)
                return false;

            if (m_ManaCost > 0 && dude.Mana < m_ManaCost)
                return false;

            return true;
        }

        public bool TryExecute(DudeCreature dude, Mobile target)
        {
            if (!CanExecute(dude, target))
                return false;

            if (m_ManaCost > 0)
                dude.Mana -= m_ManaCost;

            Execute(dude, target);
            dude.NextAbilityTime = DateTime.UtcNow + Cooldown;
            return true;
        }

        public abstract void Execute(DudeCreature dude, Mobile target);
    }
}
