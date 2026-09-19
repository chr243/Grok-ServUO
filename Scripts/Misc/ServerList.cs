#region References
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;

using Server.Network;
#endregion

namespace Server.Misc
{
	public class ServerList
    {
        public static string ServerName = Config.Get("Server.Name", "My Shard");

        public static IPAddress Address => Config.Get("Server.Address", IPAddress.Loopback);

		/// <summary>Hardcoded last-resort public IPv4 for remote clients (Contabo).</summary>
		private static readonly IPAddress FallbackPublicAddress = IPAddress.Parse("161.97.74.234");

        public static void Initialize()
        {
			EventSink.ServerList += EventSink_ServerList;
		}

		private static void EventSink_ServerList(ServerListEventArgs e)
		{
			try
			{
				var ns = e.State;
				var s = ns.Socket;
				var localEp = (IPEndPoint)s.LocalEndPoint;
				var remoteEp = (IPEndPoint)s.RemoteEndPoint;
				int port = localEp.Port; // or Config port — keep listener port

				IPAddress advertise;
				if (IPAddress.IsLoopback(remoteEp.Address))
				{
					// local testing — loopback remotes may still get 127.0.0.1
					advertise = IPAddress.Loopback;
				}
				else
				{
					advertise = ResolvePublicAdvertiseAddress(localEp.Address);

					// Never advertise loopback / Any / 0.0.0.0 to remote clients
					if (advertise == null
						|| IPAddress.IsLoopback(advertise)
						|| advertise.Equals(IPAddress.Any)
						|| advertise.Equals(IPAddress.IPv6Any))
					{
						advertise = FallbackPublicAddress;
					}
				}

				e.AddServer(ServerName, new IPEndPoint(advertise, port));
			}
			catch
			{
				e.Rejected = true;
			}
		}

		private static IPAddress ResolvePublicAdvertiseAddress(IPAddress localAddress)
		{
			// a) Prefer LocalEndPoint if public IPv4
			if (IsPublicIPv4(localAddress))
				return localAddress;

			// b) Config Server.Address if public IPv4
			IPAddress cfg = Address;
			if (IsPublicIPv4(cfg))
				return cfg;

			// c) Hardcoded public IP as last resort (never Rejected solely for missing public IP)
			return FallbackPublicAddress;
		}

		private static bool IsPublicIPv4(IPAddress ip)
		{
			if (ip == null || ip.AddressFamily != AddressFamily.InterNetwork)
				return false;
			if (IPAddress.IsLoopback(ip) || ip.Equals(IPAddress.Any))
				return false;
			if (IsPrivateNetwork(ip))
				return false;
			return true;
		}

		private static bool IsPrivateNetwork(IPAddress ip)
		{
			// 10.0.0.0/8
			// 172.16.0.0/12
			// 192.168.0.0/16
			// 169.254.0.0/16
			// 100.64.0.0/10 RFC 6598

			if (ip.AddressFamily == AddressFamily.InterNetworkV6)
			{
				return false;
			}

			if (Utility.IPMatch("192.168.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("10.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("172.16-31.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("169.254.*", ip))
			{
				return true;
			}

			if (Utility.IPMatch("100.64-127.*", ip))
			{
				return true;
			}

			return false;
		}
	}
}
