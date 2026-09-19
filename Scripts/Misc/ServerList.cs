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
					// local testing
					advertise = IPAddress.Loopback;
				}
				else
				{
					advertise = ResolvePublicAdvertiseAddress(localEp.Address);
					if (advertise == null || IPAddress.IsLoopback(advertise) || advertise.Equals(IPAddress.Any))
					{
						e.Rejected = true;
						return;
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
			// a) LocalEndPoint if public IPv4 (not loopback, not Any, not private)
			if (localAddress != null && localAddress.AddressFamily == AddressFamily.InterNetwork
				&& !IPAddress.IsLoopback(localAddress) && !localAddress.Equals(IPAddress.Any)
				&& !IsPrivateNetwork(localAddress))
				return localAddress;

			// b) Config Server.Address if public IPv4
			IPAddress cfg = Address;
			if (cfg != null && cfg.AddressFamily == AddressFamily.InterNetwork
				&& !IPAddress.IsLoopback(cfg) && !cfg.Equals(IPAddress.Any)
				&& !IsPrivateNetwork(cfg))
				return cfg;

			// c) First non-loopback non-private IPv4 on machine
			foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
			{
				if (ni.OperationalStatus != OperationalStatus.Up) continue;
				foreach (UnicastIPAddressInformation uni in ni.GetIPProperties().UnicastAddresses)
				{
					IPAddress ip = uni.Address;
					if (ip.AddressFamily != AddressFamily.InterNetwork) continue;
					if (IPAddress.IsLoopback(ip) || IsPrivateNetwork(ip)) continue;
					return ip;
				}
			}
			return null;
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
