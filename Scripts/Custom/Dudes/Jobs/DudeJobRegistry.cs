using System;
using System.Collections.Generic;

namespace Server.Custom.Dudes.Jobs
{
    /// <summary>
    /// Registers jobs and resolves which job a Dude can perform.
    /// Station asks here instead of hard-coding job types.
    /// </summary>
    public static class DudeJobRegistry
    {
        private static readonly List<DudeJob> m_Jobs = new List<DudeJob>();
        private static readonly Dictionary<string, DudeJob> m_ById =
            new Dictionary<string, DudeJob>(StringComparer.OrdinalIgnoreCase);

        private static bool m_Initialized;

        public static void EnsureInitialized()
        {
            if (m_Initialized)
                return;

            m_Initialized = true;
            RegisterDefaults();
        }

        public static void Register(DudeJob job)
        {
            if (job == null || string.IsNullOrEmpty(job.Id))
                return;

            EnsureInitialized();

            if (m_ById.ContainsKey(job.Id))
            {
                DudeJob old = m_ById[job.Id];
                m_Jobs.Remove(old);
                m_ById[job.Id] = job;
                m_Jobs.Add(job);
            }
            else
            {
                m_ById.Add(job.Id, job);
                m_Jobs.Add(job);
            }
        }

        public static DudeJob Get(string id)
        {
            EnsureInitialized();

            if (string.IsNullOrEmpty(id))
                return null;

            DudeJob job;
            if (m_ById.TryGetValue(id, out job))
                return job;

            return null;
        }

        /// <summary>
        /// First registered job this Dude can perform (v1: only Earth Gathering).
        /// </summary>
        public static DudeJob GetJobForDude(DudeData data)
        {
            EnsureInitialized();

            if (data == null)
                return null;

            for (int i = 0; i < m_Jobs.Count; i++)
            {
                if (m_Jobs[i].CanPerform(data))
                    return m_Jobs[i];
            }

            return null;
        }

        public static IList<DudeJob> GetAll()
        {
            EnsureInitialized();
            return m_Jobs.AsReadOnly();
        }

        private static void RegisterDefaults()
        {
            Register(new EarthGatheringJob());
        }
    }
}
