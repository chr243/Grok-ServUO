using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes
{
    public static class DudeAbilityRegistry
    {
        private static readonly Dictionary<string, DudeAbility> m_ById =
            new Dictionary<string, DudeAbility>(StringComparer.OrdinalIgnoreCase);

        private static bool m_Initialized;

        public static void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;

            Register(new EmberBurstAbility());
            Register(new TideCrashAbility());
            Register(new StoneSlamAbility());
            Register(new GustSlashAbility());
        }

        public static void Register(DudeAbility ability)
        {
            if (ability == null || string.IsNullOrEmpty(ability.Id))
                return;

            EnsureInitialized();
            m_ById[ability.Id] = ability;
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
    }
}
