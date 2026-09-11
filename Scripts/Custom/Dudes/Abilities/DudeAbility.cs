using System;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Extensible Dude ability. Name, cooldown, optional mana cost, Execute().
    /// </summary>
    public abstract class DudeAbility
    {
        private readonly string m_Id;
        private readonly string m_Name;
        private readonly TimeSpan m_Cooldown;
        private readonly int m_ManaCost;

        protected DudeAbility(string id, string name, TimeSpan cooldown, int manaCost)
        {
            m_Id = id;
            m_Name = name;
            m_Cooldown = cooldown;
            m_ManaCost = manaCost;
        }

        public string Id { get { return m_Id; } }
        public string Name { get { return m_Name; } }
        public TimeSpan Cooldown { get { return m_Cooldown; } }
        public int ManaCost { get { return m_ManaCost; } }

        public virtual bool CanExecute(DudeCreature dude, Mobile target)
        {
            if (dude == null || dude.Deleted || target == null || target.Deleted || !target.Alive)
                return false;

            if (dude.IsWild)
                return false;

            if (DateTime.UtcNow < dude.NextAbilityTime)
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
            dude.NextAbilityTime = DateTime.UtcNow + m_Cooldown;
            return true;
        }

        public abstract void Execute(DudeCreature dude, Mobile target);
    }
}
