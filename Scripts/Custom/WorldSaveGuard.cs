using System;
using System.IO;

namespace Server.Custom
{
    /// <summary>
    /// Ensures world saves on shutdown, and on demand via Saves/REQUEST_SAVE flag file.
    /// Touch REQUEST_SAVE; when save finishes, REQUEST_SAVE.done appears and the request is removed.
    /// </summary>
    public static class WorldSaveGuard
    {
        private static readonly string RequestPath = Path.Combine(Core.BaseDirectory, "Saves", "REQUEST_SAVE");
        private static readonly string DonePath = Path.Combine(Core.BaseDirectory, "Saves", "REQUEST_SAVE.done");
        private static Timer m_Timer;
        private static bool m_Saving;

        public static void Initialize()
        {
            EventSink.Shutdown += OnShutdown;
            m_Timer = Timer.DelayCall(TimeSpan.FromSeconds(2.0), TimeSpan.FromSeconds(2.0), CheckRequest);
        }

        private static void OnShutdown(ShutdownEventArgs e)
        {
            TrySave("shutdown");
        }

        private static void CheckRequest()
        {
            try
            {
                if (!File.Exists(RequestPath))
                    return;

                TrySave("request");
            }
            catch
            {
            }
        }

        private static void TrySave(string reason)
        {
            if (m_Saving || World.Saving)
                return;

            m_Saving = true;

            try
            {
                if (File.Exists(DonePath))
                    File.Delete(DonePath);

                if (File.Exists(RequestPath))
                    File.Delete(RequestPath);

                Console.WriteLine("WorldSaveGuard: saving ({0})...", reason);
                World.Save(true, false);
                File.WriteAllText(DonePath, DateTime.UtcNow.ToString("o"));
                Console.WriteLine("WorldSaveGuard: save finished ({0}).", reason);
            }
            catch (Exception ex)
            {
                Console.WriteLine("WorldSaveGuard: save failed: {0}", ex.Message);
            }
            finally
            {
                m_Saving = false;
            }
        }
    }
}
