using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes
{
    public static class DudeAbilityRegistry
    {
        private static readonly Dictionary<string, DudeAbility> m_ById =
            new Dictionary<string, DudeAbility>(StringComparer.OrdinalIgnoreCase);

        private static readonly List<DudeAbility> m_All = new List<DudeAbility>();

        private static bool m_Initialized;

        public static void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            // Fire: Blast → Ring of Fire → Burn
            Register(new BlastAbility());
            Register(new RingOfFireAbility());
            Register(new BurnAbility());

            // Water: Tide Mend → Tide Chorus → Spring
            Register(new TideMendAbility());
            Register(new TideChorusAbility());
            Register(new SpringAbility());

            // Earth: Fault Strike → Aftershock → Faultline
            Register(new FaultStrikeAbility());
            Register(new AftershockAbility());
            Register(new FaultlineAbility());

            // Air: Tailwind Self → Tailwind → Slipstream
            Register(new TailwindSelfAbility());
            Register(new TailwindAbility());
            Register(new SlipstreamAbility());
        }

        public static void Register(DudeAbility ability)
        {
            if (ability == null || string.IsNullOrEmpty(ability.Id))
                return;

            EnsureInitialized();
            m_ById[ability.Id] = ability;

            bool found = false;
            for (int i = 0; i < m_All.Count; i++)
            {
                if (string.Equals(m_All[i].Id, ability.Id, StringComparison.OrdinalIgnoreCase))
                {
                    m_All[i] = ability;
                    found = true;
                    break;
                }
            }
            if (!found)
                m_All.Add(ability);
        }

        public static DudeAbility Get(string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                return null;

            DudeAbility ability;
            if (m_ById.TryGetValue(id, out ability))
                return ability;

            return null;
        }

        public static DudeAbility GetByKitStage(DudeType kit, int stage)
        {
            EnsureInitialized();

            for (int i = 0; i < m_All.Count; i++)
            {
                DudeAbility a = m_All[i];
                if (a.Kit == kit && a.Stage == stage)
                    return a;
            }

            return null;
        }

        public static IList<DudeAbility> GetAll()
        {
            EnsureInitialized();
            return m_All.AsReadOnly();
        }
    }
}
