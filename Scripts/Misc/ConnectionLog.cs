using System;
using System.IO;

namespace Server.Misc
{
	public static class ConnectionLog
	{
		public static void Write(string format, params object[] args)
		{
			try
			{
				if (!Directory.Exists("Logs"))
					Directory.CreateDirectory("Logs");

				string msg = args != null && args.Length > 0 ? string.Format(format, args) : format;
				string line = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg;

				using (StreamWriter w = new StreamWriter("Logs/Connections.log", true))
				{
					w.WriteLine(line);
					w.Flush();
				}
			}
			catch
			{ }
		}
	}
}
