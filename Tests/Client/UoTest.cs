// Minimal headless UO client for smoke/latency/stress testing a local ServUO shard.
// Protocol: 0xEF seed + 0x80 login -> 0xA8 -> 0xA0 -> 0x8C relay; game connection with
// Huffman-compressed server stream (each packet compressed separately, ending in symbol 256).
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UoTest
{
    internal static partial class HuffmanTable
    {
    }

    internal sealed class HuffmanDecoder
    {
        private readonly Dictionary<int, int> _codes = new Dictionary<int, int>();
        private readonly List<byte> _packet = new List<byte>(512);
        private int _code, _len;

        public HuffmanDecoder()
        {
            for (int s = 0; s < 257; s++)
                _codes[(HuffmanTable.Table[s * 2] << 16) | HuffmanTable.Table[s * 2 + 1]] = s;
        }

        public void Feed(byte[] data, int count, List<byte[]> output)
        {
            for (int i = 0; i < count; i++)
            {
                int b = data[i];

                for (int bit = 7; bit >= 0; bit--)
                {
                    _code = (_code << 1) | ((b >> bit) & 1);
                    _len++;

                    int sym;
                    if (_codes.TryGetValue((_len << 16) | _code, out sym))
                    {
                        _code = 0;
                        _len = 0;

                        if (sym == 256)
                        {
                            output.Add(_packet.ToArray());
                            _packet.Clear();
                            break; // remainder of this byte is padding
                        }

                        _packet.Add((byte)sym);
                    }
                    else if (_len > 11)
                    {
                        throw new InvalidDataException("bad huffman code");
                    }
                }
            }
        }
    }

    internal sealed class Stats
    {
        public long Packets, WireBytes, RecvCalls, DecodedBytes, SpeechHeard, MovesHeard;
        public int MoveRejects;
        public double MaxGapMs;
        public readonly Dictionary<int, long> ById = new Dictionary<int, long>();
        public readonly List<double> MoveRtt = new List<double>();
    }

    internal sealed class Client
    {
        private static readonly double TickMs = 1000.0 / Stopwatch.Frequency;

        private readonly string _host;
        private readonly int _port;
        private readonly object _sendLock = new object();
        private readonly Dictionary<byte, long> _pending = new Dictionary<byte, long>();
        private readonly ManualResetEvent _charList = new ManualResetEvent(false);
        private readonly ManualResetEvent _loginDone = new ManualResetEvent(false);
        private readonly List<string> _messages = new List<string>();

        public readonly string Account, Password;
        public readonly Stats Stats = new Stats();

        private Socket _sock;
        private HuffmanDecoder _decoder;
        private volatile bool _running;
        private byte[] _charListData;
        private byte _seq;
        private long _lastPacket;
        public volatile bool TrackGaps;
        public volatile string Error;
        public int Serial, X, Y, Z;

        public Client(string host, int port, string account, string password)
        {
            _host = host;
            _port = port;
            Account = account;
            Password = password;
        }

        public List<string> Messages { get { lock (_messages) return new List<string>(_messages); } }

        // First time each mobile serial was seen in a 0x78 (MobileIncoming), as a Stopwatch tick.
        private readonly Dictionary<int, long> _mobileSeen = new Dictionary<int, long>();

        public List<KeyValuePair<int, long>> MobilesSeenAfter(long tick)
        {
            lock (_mobileSeen)
                return _mobileSeen.Where(kv => kv.Value > tick).OrderBy(kv => kv.Value).ToList();
        }

        public void StartGapTracking()
        {
            lock (Stats)
            {
                _lastPacket = 0;
                Stats.MaxGapMs = 0;
            }

            TrackGaps = true;
        }

        private Socket Connect()
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp) { NoDelay = true, ReceiveTimeout = 10000 };
            s.Connect(_host, _port);
            return s;
        }

        private static void U32(byte[] b, int o, uint v) { b[o] = (byte)(v >> 24); b[o + 1] = (byte)(v >> 16); b[o + 2] = (byte)(v >> 8); b[o + 3] = (byte)v; }
        private static uint U32(byte[] b, int o) { return (uint)((b[o] << 24) | (b[o + 1] << 16) | (b[o + 2] << 8) | b[o + 3]); }
        private static int U16(byte[] b, int o) { return (b[o] << 8) | b[o + 1]; }
        private static void Ascii(byte[] b, int o, string s, int len) { var a = Encoding.ASCII.GetBytes(s); Buffer.BlockCopy(a, 0, b, o, Math.Min(a.Length, len)); }

        private static byte[] ReadExact(Socket s, int n)
        {
            var buf = new byte[n];
            int got = 0;
            while (got < n)
            {
                int r = s.Receive(buf, got, n - got, SocketFlags.None);
                if (r <= 0) throw new IOException("connection closed during login");
                got += r;
            }
            return buf;
        }

        public void Login(int timeoutMs)
        {
            Login(timeoutMs, false);
        }

        public void Login(int timeoutMs, bool charListOnly)
        {
            uint key;

            using (var s = Connect())
            {
                var seed = new byte[21];
                seed[0] = 0xEF;
                U32(seed, 1, 0x7F000001);
                U32(seed, 5, 7); U32(seed, 9, 0); U32(seed, 13, 50); U32(seed, 17, 0); // client 7.0.50.0
                s.Send(seed);

                var login = new byte[62];
                login[0] = 0x80;
                Ascii(login, 1, Account, 30);
                Ascii(login, 31, Password, 30);
                login[61] = 0x5D;
                s.Send(login);

                int id = ReadExact(s, 1)[0];
                if (id == 0x82) throw new Exception("login denied, reason " + ReadExact(s, 1)[0]);
                if (id != 0xA8) throw new Exception("unexpected login reply 0x" + id.ToString("X2"));
                var len = ReadExact(s, 2);
                ReadExact(s, U16(len, 0) - 3);

                s.Send(new byte[] { 0xA0, 0x00, 0x00 });

                var relay = ReadExact(s, 11);
                if (relay[0] != 0x8C) throw new Exception("expected 0x8C relay, got 0x" + relay[0].ToString("X2"));
                key = U32(relay, 7);
            }

            _sock = Connect();
            _sock.ReceiveTimeout = 0; // game connection can be quiet for long stretches

            var auth = new byte[4];
            U32(auth, 0, key);
            var gameLogin = new byte[65];
            gameLogin[0] = 0x91;
            U32(gameLogin, 1, key);
            Ascii(gameLogin, 5, Account, 30);
            Ascii(gameLogin, 35, Password, 30);

            _decoder = new HuffmanDecoder();
            _running = true;
            new Thread(ReadLoop) { IsBackground = true, Name = "recv-" + Account }.Start();

            Send(auth);
            Send(gameLogin);

            if (!_charList.WaitOne(timeoutMs) || _charListData == null)
                throw new TimeoutException("no character list (" + (Error ?? "timeout") + ") key=0x" + key.ToString("X8"));

            if (charListOnly)
                return;

            bool hasChar = _charListData.Length > 5 && _charListData[4] != 0;

            if (hasChar)
                SendPlay();
            else
                SendCreate();

            if (!_loginDone.WaitOne(timeoutMs) || Error != null)
                throw new TimeoutException("no login complete (" + (Error ?? "timeout") + ")");
        }

        private void SendPlay()
        {
            var p = new byte[73];
            p[0] = 0x5D;
            U32(p, 1, 0xEDEDEDED);
            Ascii(p, 5, Account, 30);
            U32(p, 65, 0);          // slot
            U32(p, 69, 0x7F000001); // client ip
            Send(p);
        }

        private void SendCreate()
        {
            var p = new byte[106];
            p[0] = 0xF8;
            U32(p, 1, 0xEDEDEDED);
            U32(p, 5, 0xFFFFFFFF);
            Ascii(p, 10, Account, 30);
            p[70] = 0;  // human male
            p[71] = 45; p[72] = 35; p[73] = 10;               // str/dex/int = 90
            p[74] = 0; p[75] = 50; p[76] = 1; p[77] = 50;      // skills
            p[78] = 2; p[79] = 0; p[80] = 3; p[81] = 0;
            p[82] = 0x83; p[83] = 0xEA;                        // skin
            p[84] = 0x20; p[85] = 0x3B; p[86] = 0x04; p[87] = 0x4E; // hair
            p[93] = 0;                                         // city index
            U32(p, 94, 0);                                     // slot
            U32(p, 98, 0x7F000001);
            Send(p);
        }

        private void SendVersion()
        {
            var v = Encoding.ASCII.GetBytes("7.0.50.0\0");
            var p = new byte[3 + v.Length];
            p[0] = 0xBD; p[1] = (byte)(p.Length >> 8); p[2] = (byte)p.Length;
            Buffer.BlockCopy(v, 0, p, 3, v.Length);
            Send(p);
        }

        public void Send(byte[] p)
        {
            lock (_sendLock)
                _sock.Send(p);
        }

        public void Step(int dir)
        {
            byte seq;

            lock (_pending)
            {
                seq = _seq;
                _pending[seq] = Stopwatch.GetTimestamp();
                _seq = (byte)(_seq == 255 ? 1 : _seq + 1);
            }

            Send(new byte[] { 0x02, (byte)dir, seq, 0, 0, 0, 0 });
        }

        public void Say(string text)
        {
            var t = Encoding.BigEndianUnicode.GetBytes(text);
            int len = 12 + t.Length + 2;
            var p = new byte[len];
            p[0] = 0xAD; p[1] = (byte)(len >> 8); p[2] = (byte)len;
            p[3] = 0; p[4] = 0x00; p[5] = 0x34; p[6] = 0; p[7] = 3;
            Encoding.ASCII.GetBytes("ENU\0", 0, 4, p, 8);
            Buffer.BlockCopy(t, 0, p, 12, t.Length);
            Send(p);
        }

        private void ReadLoop()
        {
            var buf = new byte[65536];
            var packets = new List<byte[]>();

            try
            {
                while (_running)
                {
                    int n = _sock.Receive(buf);
                    if (n <= 0)
                    {
                        if (_running) Error = Error ?? "disconnected by server";
                        break;
                    }

                    long now = Stopwatch.GetTimestamp();

                    lock (Stats)
                    {
                        Stats.RecvCalls++;
                        Stats.WireBytes += n;
                    }

                    packets.Clear();
                    _decoder.Feed(buf, n, packets);

                    foreach (var p in packets)
                        Handle(p, now);
                }
            }
            catch (Exception ex)
            {
                if (_running) Error = Error ?? ex.GetType().Name + ": " + ex.Message;
            }
            finally
            {
                _charList.Set();
                _loginDone.Set();
            }
        }

        private void Handle(byte[] p, long now)
        {
            if (p.Length == 0) return;
            int id = p[0];

            lock (Stats)
            {
                Stats.Packets++;
                Stats.DecodedBytes += p.Length;
                long c;
                Stats.ById.TryGetValue(id, out c);
                Stats.ById[id] = c + 1;

                if (TrackGaps && _lastPacket != 0)
                {
                    double gap = (now - _lastPacket) * TickMs;
                    if (gap > Stats.MaxGapMs) Stats.MaxGapMs = gap;
                }

                _lastPacket = now;
            }

            switch (id)
            {
                case 0xA9:
                    _charListData = p;
                    _charList.Set();
                    break;
                case 0x1B:
                    if (p.Length >= 17) { Serial = (int)U32(p, 1); X = U16(p, 11); Y = U16(p, 13); Z = (short)U16(p, 15); }
                    break;
                case 0x55:
                    _loginDone.Set();
                    break;
                case 0x22:
                case 0x21:
                    {
                        long sent;
                        bool found;

                        lock (_pending)
                        {
                            found = _pending.TryGetValue(p[1], out sent);
                            if (id == 0x21) { _pending.Clear(); _seq = 0; }
                            else if (found) _pending.Remove(p[1]);
                        }

                        lock (Stats)
                        {
                            if (found) Stats.MoveRtt.Add((now - sent) * TickMs);
                            if (id == 0x21) Stats.MoveRejects++;
                        }
                        break;
                    }
                case 0xBD:
                    SendVersion();
                    break;
                case 0x77:
                    lock (Stats) Stats.MovesHeard++;
                    break;
                case 0x78:
                    if (p.Length >= 7)
                    {
                        int serial = (int)U32(p, 3);
                        lock (_mobileSeen)
                        {
                            if (!_mobileSeen.ContainsKey(serial))
                                _mobileSeen[serial] = now;
                        }
                    }
                    break;
                case 0xAE:
                    {
                        lock (Stats) Stats.SpeechHeard++;
                        if (p.Length > 48)
                        {
                            string text = Encoding.BigEndianUnicode.GetString(p, 48, p.Length - 48).TrimEnd('\0');
                            lock (_messages) _messages.Add(text);
                        }
                        break;
                    }
                case 0x1C:
                    {
                        lock (Stats) Stats.SpeechHeard++;
                        if (p.Length > 44)
                        {
                            string text = Encoding.ASCII.GetString(p, 44, p.Length - 44).TrimEnd('\0');
                            lock (_messages) _messages.Add(text);
                        }
                        break;
                    }
                case 0x82:
                case 0x85:
                case 0x53:
                    Error = Error ?? "server rejected with 0x" + id.ToString("X2") + (p.Length > 1 ? " code " + p[1] : "");
                    _loginDone.Set();
                    break;
            }
        }

        public void Close()
        {
            _running = false;
            try { _sock.Shutdown(SocketShutdown.Both); } catch { }
            try { _sock.Close(); } catch { }
        }
    }

    internal static class Program
    {
        private static Dictionary<string, string> _opts;

        private static string Opt(string name, string def) { string v; return _opts.TryGetValue(name, out v) ? v : def; }
        private static int OptInt(string name, int def) { return int.Parse(Opt(name, def.ToString())); }

        private static int Main(string[] args)
        {
            _opts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < args.Length; i += 2)
                _opts[args[i].TrimStart('-')] = args[i + 1];

            string host = Opt("host", "127.0.0.1");
            int port = OptInt("port", 2593);
            string mode = Opt("mode", "walk");

            try
            {
                switch (mode)
                {
                    case "walk": return Walk(host, port);
                    case "stress": return Stress(host, port);
                    case "say": return SayScript(host, port);
                    case "relay": return Relay(host, port);
                    case "spawnwatch": return SpawnWatch(host, port);
                    default: Console.WriteLine("unknown mode"); return 2;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("FAILED: " + ex.Message);
                return 1;
            }
        }

        private static string Summ(List<double> v)
        {
            if (v.Count == 0) return "n=0";
            var s = v.OrderBy(x => x).ToList();
            Func<double, double> pct = q => s[Math.Min(s.Count - 1, (int)Math.Floor(q * (s.Count - 1) + 0.5))];
            return string.Format("n={0} avg={1:0.000}ms p50={2:0.000} p95={3:0.000} p99={4:0.000} max={5:0.000}",
                s.Count, s.Average(), pct(0.50), pct(0.95), pct(0.99), s[s.Count - 1]);
        }

        private static void Report(string label, IList<Client> clients, double seconds)
        {
            long packets = 0, wire = 0, recv = 0, decoded = 0, speech = 0, moves = 0;
            int rejects = 0, errors = 0;
            double maxGap = 0;
            var rtt = new List<double>();

            foreach (var c in clients)
            {
                lock (c.Stats)
                {
                    packets += c.Stats.Packets; wire += c.Stats.WireBytes; recv += c.Stats.RecvCalls;
                    decoded += c.Stats.DecodedBytes; speech += c.Stats.SpeechHeard; moves += c.Stats.MovesHeard;
                    rejects += c.Stats.MoveRejects; rtt.AddRange(c.Stats.MoveRtt);
                    maxGap = Math.Max(maxGap, c.Stats.MaxGapMs);
                }

                if (c.Error != null)
                {
                    errors++;
                    Console.WriteLine("  client {0} error: {1}", c.Account, c.Error);
                }
            }

            Console.WriteLine("[{0}] clients={1} duration={2:0.0}s errors={3}", label, clients.Count, seconds, errors);
            Console.WriteLine("  packets={0} decodedBytes={1} wireBytes={2} recvCalls={3} bytes/recv={4:0.0} compression={5:0.00}",
                packets, decoded, wire, recv, recv > 0 ? (double)wire / recv : 0, decoded > 0 ? (double)wire / decoded : 0);
            Console.WriteLine("  speechHeard={0} movesHeard={1} moveRejects={2} maxGapBetweenPackets={3:0.0}ms", speech, moves, rejects, maxGap);
            Console.WriteLine("  moveAckRTT " + Summ(rtt));

            var ids = new Dictionary<int, long>();
            foreach (var c in clients)
                lock (c.Stats)
                    foreach (var kv in c.Stats.ById)
                    {
                        long n;
                        ids.TryGetValue(kv.Key, out n);
                        ids[kv.Key] = n + kv.Value;
                    }

            Func<int, long> count = id => { long n; return ids.TryGetValue(id, out n) ? n : 0; };
            Console.WriteLine("  healthbar 0x17={0} 0x16={1}  mobileIncoming 0x78={2}  tooltip revisions 0xDC={3}",
                count(0x17), count(0x16), count(0x78), count(0xDC));
        }

        private static int Walk(string host, int port)
        {
            var c = new Client(host, port, Opt("account", "walker"), Opt("password", "pw"));
            var sw = Stopwatch.StartNew();
            c.Login(20000);
            Console.WriteLine("login ok in {0} ms (serial 0x{1:X}, at {2},{3},{4})", sw.ElapsedMilliseconds, c.Serial, c.X, c.Y, c.Z);

            Thread.Sleep(1500);

            int steps = OptInt("steps", 60), interval = OptInt("interval", 450);
            sw.Restart();
            for (int i = 0; i < steps && c.Error == null; i++)
            {
                int dir = ((i / 4) % 2 == 0) ? 2 : 6; // east x4, west x4 ...
                c.Step(dir);
                Thread.Sleep(interval);
            }
            Thread.Sleep(1000);

            Report("walk", new[] { c }, sw.Elapsed.TotalSeconds);

            lock (c.Stats)
                Console.WriteLine("  by id: " + string.Join(" ", c.Stats.ById.OrderBy(kv => kv.Key).Select(kv => kv.Key.ToString("X2") + "x" + kv.Value)));

            c.Close();
            return c.Error == null ? 0 : 1;
        }

        private static int Stress(string host, int port)
        {
            int count = OptInt("clients", 10), seconds = OptInt("seconds", 30);
            int moveMs = OptInt("moveMs", 450), sayMs = OptInt("sayMs", 400);
            int sayLen = OptInt("sayLen", 0);          // 0 = random short lines
            bool sync = OptInt("sync", 0) != 0;        // all clients speak in the same instant
            string prefix = Opt("prefix", "stress");

            var clients = new List<Client>();
            for (int i = 0; i < count; i++)
            {
                var c = new Client(host, port, prefix + i, "pw");
                c.Login(30000);
                clients.Add(c);
            }

            Console.WriteLine("{0} clients logged in", clients.Count);
            Thread.Sleep(2000);

            foreach (var c in clients)
                lock (c.Stats) { c.Stats.MoveRtt.Clear(); }

            foreach (var c in clients) c.StartGapTracking();

            var stop = DateTime.UtcNow.AddSeconds(seconds);
            var threads = new List<Thread>();
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < clients.Count; i++)
            {
                var c = clients[i];
                int idx = i;
                var t = new Thread(() =>
                {
                    var rnd = new Random(idx * 7919 + 1);
                    long nextMove = 0, nextSay = rnd.Next(sayMs), lastSlot = -1;
                    int step = 0;
                    var local = Stopwatch.StartNew();

                    while (DateTime.UtcNow < stop && c.Error == null)
                    {
                        long now = local.ElapsedMilliseconds;

                        if (now >= nextMove)
                        {
                            int dir = ((step / 3) % 2 == 0) ? (idx % 2 == 0 ? 2 : 4) : (idx % 2 == 0 ? 6 : 0);
                            c.Step(dir);
                            step++;
                            nextMove = now + moveMs;
                        }

                        bool speak;
                        if (sync)
                        {
                            long slot = DateTime.UtcNow.Ticks / (sayMs * TimeSpan.TicksPerMillisecond);
                            speak = slot != lastSlot;
                            lastSlot = slot;
                        }
                        else
                        {
                            speak = now >= nextSay;
                        }

                        if (speak)
                        {
                            string line = "stress " + idx + " says hello number " + step + " ";
                            line += sayLen > 0 ? new string('x', Math.Max(1, sayLen - line.Length)) : new string('x', rnd.Next(10, 60));
                            c.Say(line);
                            nextSay = now + sayMs;
                        }

                        Thread.Sleep(sync ? 1 : 5);
                    }
                }) { IsBackground = true };
                threads.Add(t);
                t.Start();
            }

            foreach (var t in threads) t.Join();
            Thread.Sleep(1000);
            foreach (var c in clients) c.TrackGaps = false;

            Report("stress", clients, sw.Elapsed.TotalSeconds);
            int errs = clients.Count(c => c.Error != null);
            foreach (var c in clients) c.Close();
            return errs == 0 ? 0 : 1;
        }

        // DudeSpawner respawn timing: create spawners, shorten their delays, wait until the first
        // scheduled respawn time is stale while full, kill every Dude, and log when new mobiles appear.
        private static int SpawnWatch(string host, int port)
        {
            int staleSeconds = OptInt("stale", 320), watchSeconds = OptInt("watch", 45);
            var c = new Client(host, port, Opt("account", "admin"), Opt("password", "adminpw"));
            c.Login(20000);
            Thread.Sleep(1500);

            foreach (var cmd in new[] { "[CreateDudeSpawners", "[Global Set MinDelay 00:00:15 where DudeSpawner", "[Global Set MaxDelay 00:00:20 where DudeSpawner" })
            {
                c.Say(cmd);
                Thread.Sleep(2500);
            }

            Console.WriteLine("spawners created; waiting {0}s so the first respawn time goes stale while full", staleSeconds);
            Thread.Sleep(staleSeconds * 1000);

            long t0 = Stopwatch.GetTimestamp();
            c.Say("[Global Kill where DudeCreature");
            Thread.Sleep(watchSeconds * 1000);

            var seen = c.MobilesSeenAfter(t0);
            double tickMs = 1000.0 / Stopwatch.Frequency;
            Console.WriteLine("new mobiles within {0}s after the kill: {1}", watchSeconds, seen.Count);
            if (seen.Count > 0)
            {
                Console.WriteLine("  first at +{0:0.0}s, last at +{1:0.0}s",
                    (seen[0].Value - t0) * tickMs / 1000.0, (seen[seen.Count - 1].Value - t0) * tickMs / 1000.0);
                Console.WriteLine("  arrivals (s): " + string.Join(" ", seen.Select(kv => ((kv.Value - t0) * tickMs / 1000.0).ToString("0.0"))));
            }

            foreach (var m in c.Messages.Distinct().Take(20))
                Console.WriteLine("< " + m);

            if (c.Error != null)
                Console.WriteLine("client error: " + c.Error);

            lock (c.Stats)
                Console.WriteLine("packet ids seen: " + string.Join(" ", c.Stats.ById.OrderBy(kv => kv.Key).Select(kv => kv.Key.ToString("X2") + "x" + kv.Value)));

            c.Close();
            return c.Error == null ? 0 : 1;
        }

        // Repeats login -> relay -> game login up to the character list; reports failed relay keys.
        private static int Relay(string host, int port)
        {
            int count = OptInt("count", 500), failures = 0;
            var failed = new List<string>();
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < count; i++)
            {
                var c = new Client(host, port, Opt("prefix", "relay") + (i % 3), "pw");

                try
                {
                    c.Login(10000, true);
                }
                catch (Exception ex)
                {
                    failures++;
                    failed.Add(ex.Message);
                }
                finally
                {
                    c.Close();
                }
            }

            Console.WriteLine("[relay] attempts={0} failures={1} time={2:0.0}s", count, failures, sw.Elapsed.TotalSeconds);
            foreach (var f in failed.Take(10)) Console.WriteLine("  " + f);
            return 0;
        }

        // Script lines separated by '|': plain text is spoken; "~N" sleeps N ms; "?text" waits (up to
        // -waitMs) until a received message contains text. Messages print as they arrive.
        private static int SayScript(string host, int port)
        {
            var c = new Client(host, port, Opt("account", "admin"), Opt("password", "adminpw"));
            c.Login(20000);
            Console.WriteLine("login ok as {0}", c.Account);
            Thread.Sleep(1500);

            int printed = 0;
            Action drain = () =>
            {
                var all = c.Messages;
                for (; printed < all.Count; printed++) Console.WriteLine("< " + all[printed]);
            };

            foreach (var line in Opt("lines", "").Split('|'))
            {
                if (line.Length == 0) continue;

                if (line[0] == '~')
                {
                    Thread.Sleep(int.Parse(line.Substring(1)));
                }
                else if (line[0] == '?')
                {
                    string want = line.Substring(1);
                    var until = DateTime.UtcNow.AddMilliseconds(OptInt("waitMs", 120000));
                    while (DateTime.UtcNow < until && c.Error == null && !c.Messages.Any(m => m.Contains(want)))
                    {
                        drain();
                        Thread.Sleep(100);
                    }
                    if (!c.Messages.Any(m => m.Contains(want))) Console.WriteLine("TIMEOUT waiting for: " + want);
                }
                else
                {
                    c.Say(line);
                    Console.WriteLine("> " + line);
                    Thread.Sleep(OptInt("delay", 2000));
                }

                drain();
            }

            Thread.Sleep(1500);
            drain();

            lock (c.Stats)
                Console.WriteLine("packet ids seen: " + string.Join(" ", c.Stats.ById.OrderBy(kv => kv.Key).Select(kv => kv.Key.ToString("X2") + "x" + kv.Value)));

            if (c.Error != null) Console.WriteLine("client error: " + c.Error);
            c.Close();
            return c.Error == null ? 0 : 1;
        }
    }
}
