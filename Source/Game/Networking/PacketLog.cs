// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Framework.Configuration;
using Framework.Constants;
using Game;

public class PacketLog
{
	static readonly string FullPath;
	static readonly ConcurrentQueue<(byte[], uint, IPEndPoint, ConnectionType, bool)> PacketQueue = new();
	static readonly AutoResetEvent QueueSemaphore = new(false);

	static PacketLog()
	{
		var logsDir = AppContext.BaseDirectory + ConfigMgr.GetDefaultValue("LogsDir", "");
		var logname = ConfigMgr.GetDefaultValue("PacketLogFile", "");

		if (!string.IsNullOrEmpty(logname))
		{
			FullPath = logsDir + @"\" + logname;
			using var writer = new BinaryWriter(File.Open(FullPath, FileMode.Create));
			writer.Write(Encoding.ASCII.GetBytes("PKT"));
			writer.Write((ushort)769);
			writer.Write(Encoding.ASCII.GetBytes("T"));
			writer.Write(Global.WorldMgr.Realm.Build);
			writer.Write(Encoding.ASCII.GetBytes("enUS"));
			writer.Write(new byte[40]); //SessionKey
			writer.Write((uint)GameTime.GetGameTime());
			writer.Write(Time.MSTime);
			writer.Write(0);
		}

		Task.Run(() =>
		{
			using var writer = new BinaryWriter(File.Open(FullPath, FileMode.Append), Encoding.ASCII);

			while (!WorldManager.Instance.IsShuttingDown)
			{
				QueueSemaphore.WaitOne(500);

				while (PacketQueue.Count != 0)
					if (PacketQueue.TryDequeue(out var packet))
					{
						writer.Write(packet.Item5 ? 0x47534d43 : 0x47534d53);
						writer.Write((uint)packet.Item4);
						writer.Write(Time.MSTime);

						writer.Write(20);
						var SocketIPBytes = new byte[16];

						if (packet.Item3.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
							Buffer.BlockCopy(packet.Item3.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 4);
						else
							Buffer.BlockCopy(packet.Item3.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 16);

						var size = packet.Item1.Length;

						if (packet.Item5)
							size -= 2;

						writer.Write(size + 4);
						writer.Write(SocketIPBytes);
						writer.Write(packet.Item3.Port);
						writer.Write(packet.Item2);

						if (packet.Item5)
							writer.Write(packet.Item1, 2, size);
						else
							writer.Write(packet.Item1, 0, size);
					}
			}
		});
	}

	public static void Write(byte[] data, uint opcode, IPEndPoint endPoint, ConnectionType connectionType, bool isClientPacket)
	{
		if (!CanLog())
			return;

		PacketQueue.Enqueue((data, opcode, endPoint, connectionType, isClientPacket));
		QueueSemaphore.Set();
	}

	public static bool CanLog()
	{
		return !string.IsNullOrEmpty(FullPath);
	}
}