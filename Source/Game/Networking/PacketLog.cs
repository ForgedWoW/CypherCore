// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.IO;
using System.Net;
using System.Text;
using Framework.Configuration;
using Framework.Constants;

public class PacketLog
{
	static readonly object syncObj = new();
	static readonly string FullPath;

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
	}

	public static void Write(byte[] data, uint opcode, IPEndPoint endPoint, ConnectionType connectionType, bool isClientPacket)
	{
		if (!CanLog())
			return;

		lock (syncObj)
		{
			using var writer = new BinaryWriter(File.Open(FullPath, FileMode.Append), Encoding.ASCII);
			writer.Write(isClientPacket ? 0x47534d43 : 0x47534d53);
			writer.Write((uint)connectionType);
			writer.Write(Time.MSTime);

			writer.Write(20);
			var SocketIPBytes = new byte[16];

			if (endPoint.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
				Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 4);
			else
				Buffer.BlockCopy(endPoint.Address.GetAddressBytes(), 0, SocketIPBytes, 0, 16);

			var size = data.Length;

			if (isClientPacket)
				size -= 2;

			writer.Write(size + 4);
			writer.Write(SocketIPBytes);
			writer.Write(endPoint.Port);
			writer.Write(opcode);

			if (isClientPacket)
				writer.Write(data, 2, size);
			else
				writer.Write(data, 0, size);
		}
	}

	public static bool CanLog()
	{
		return !string.IsNullOrEmpty(FullPath);
	}
}