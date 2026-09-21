using System;

namespace Server.Custom.Dudes
{
    /// <summary>
    /// Coalesces InvalidateProperties() for per-hit / per-kill callers. Each rebuild re-runs
    /// GetProperties and sends a revision packet to every client in range (clients that cache
    /// tooltips then re-request the full list), so refresh at most once per window; a request
    /// inside the window schedules one deferred refresh so the tooltip still ends up current.
    /// </summary>
    public sealed class ThrottledPropertyRefresh
    {
        private readonly Action m_Invalidate;
        private readonly Func<bool> m_IsDeleted;
        private readonly TimeSpan m_Window;

        private DateTime m_Next;
        private Timer m_Timer;

        public ThrottledPropertyRefresh(Action invalidate, Func<bool> isDeleted, TimeSpan window)
        {
            m_Invalidate = invalidate;
            m_IsDeleted = isDeleted;
            m_Window = window;
        }

        public void Request()
        {
            if (m_Timer != null || m_IsDeleted())
                return;

            DateTime now = DateTime.UtcNow;

            if (now >= m_Next)
            {
                m_Next = now + m_Window;
                m_Invalidate();
            }
            else
            {
                m_Timer = Timer.DelayCall(m_Next - now, new TimerCallback(Flush));
            }
        }

        private void Flush()
        {
            m_Timer = null;

            if (m_IsDeleted())
                return;

            m_Next = DateTime.UtcNow + m_Window;
            m_Invalidate();
        }
    }
}
