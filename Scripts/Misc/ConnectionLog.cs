using System;
using System.IO;

namespace Server.Misc
{
	/// <summary>
	///     Appends connection events to Logs/Connections.log. Called on the main thread for every
	///     accept/connect/disconnect, so it keeps one file handle open instead of checking the
	///     directory and reopening the file for every line.
	/// </summary>
	public static class ConnectionLog
	{
		private const string FilePath = "Logs/Connections.log";

		private static readonly object _Sync = new object();

		private static StreamWriter _Writer;

		public static void Write(string format, params object[] args)
		{
			try
			{
				string msg = args != null && args.Length > 0 ? string.Format(format, args) : format;
				string line = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss") + " " + msg;

				lock (_Sync)
				{
					try
					{
						if (_Writer == null)
						{
							if (!Directory.Exists("Logs"))
								Directory.CreateDirectory("Logs");

							_Writer = new StreamWriter(
								new FileStream(FilePath, FileMode.Append, FileAccess.Write, FileShare.ReadWrite | FileShare.Delete));
						}

						_Writer.WriteLine(line);
						_Writer.Flush();
					}
					catch
					{
						// Drop the handle so the next call retries opening the file.
						if (_Writer != null)
						{
							try
							{
								_Writer.Dispose();
							}
							catch
							{ }

							_Writer = null;
						}
					}
				}
			}
			catch
			{ }
		}
	}
}
