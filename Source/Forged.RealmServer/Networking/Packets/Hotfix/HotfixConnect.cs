// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Framework.IO;
using Forged.RealmServer.DataStorage;

namespace Forged.RealmServer.Networking.Packets;

class HotfixConnect : ServerPacket
{
	public List<HotfixData> Hotfixes = new();
	public ByteBuffer HotfixContent = new();
	public HotfixConnect() : base(ServerOpcodes.HotfixConnect) { }

	public override void Write()
	{
		_worldPacket.WriteInt32(Hotfixes.Count);

		foreach (var hotfix in Hotfixes)
			hotfix.Write(_worldPacket);

		_worldPacket.WriteUInt32(HotfixContent.GetSize());
		_worldPacket.WriteBytes(HotfixContent);
	}

	public class HotfixData
	{
		public HotfixRecord Record = new();
		public uint Size;

		public void Write(WorldPacket data)
		{
			Record.Write(data);
			data.WriteUInt32(Size);
			data.WriteBits((byte)Record.HotfixStatus, 3);
			data.FlushBits();
		}
	}
}