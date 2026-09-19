using System;
using Server.Items;
using Server.Mobiles;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Extensible Dude ability. Name, cooldown, optional mana cost, kit/stage, Execute().
    /// Cooldown reads live from DudeAbilityConfig when configured.
    /// </summary>
    public abstract class DudeAbility
    {
        /// <summary>
        /// Threaded by TryUseAbility / passives from the casting DudeGear piece.
        /// Execute reads this; always reset to 1.0 after Execute.
        /// </summary>
        public static double CurrentEffectMultiplier = 1.0;

        /// <summary>Scale damage/heal by CurrentEffectMultiplier (level 0 → 0).</summary>
        public static int ApplyEffect(int value)
        {
            int scaled = (int)Math.Round(value * CurrentEffectMultiplier);
            if (scaled < 0)
                scaled = 0;
            return scaled;
        }

        /// <summary>Scale a speed factor toward 1.0 (no boost) by CurrentEffectMultiplier.</summary>
        public static double ApplyEffectSpeed(double speedFactor)
        {
            double m = CurrentEffectMultiplier;
            if (m < 0.0)
                m = 0.0;
            if (m > 1.0)
                m = 1.0;
            return 1.0 + (speedFactor - 1.0) * m;
        }

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

        /// <summary>Linked-player path: caster is the player in Dude form.</summary>
        public virtual bool CanExecuteLinked(Mobile caster, DudeData data, Mobile target)
        {
            if (caster == null || caster.Deleted || !caster.Alive)
                return false;
            if (data == null)
                return false;
            if (target == null || target.Deleted || !target.Alive)
                return false;
            if (m_ManaCost > 0 && caster.Mana < m_ManaCost)
                return false;
            return true;
        }

        public virtual bool TryExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
            if (!CanExecuteLinked(caster, data, target))
                return false;

            if (m_ManaCost > 0)
                caster.Mana -= m_ManaCost;

            ExecuteLinked(caster, data, ball, target);
            return true;
        }

        public virtual void ExecuteLinked(Mobile caster, DudeData data, DudeBall ball, Mobile target)
        {
        }
    }
}
