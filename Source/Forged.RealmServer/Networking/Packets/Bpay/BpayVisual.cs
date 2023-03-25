// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Networking.Packets.Bpay;

public class BpayVisual
{
	public uint Entry { get; set; }
	public string Name { get; set; } = "";
	public uint DisplayId { get; set; }
	public uint VisualId { get; set; }
	public uint Unk { get; set; }
	public uint DisplayInfoEntry { get; set; }

	public void Write(WorldPacket _worldPacket)
	{
		_worldPacket.WriteBits(Name.Length, 10);
		_worldPacket.FlushBits();
		_worldPacket.Write(DisplayId);
		_worldPacket.Write(VisualId);
		_worldPacket.Write(Unk);
		_worldPacket.WriteString(Name);
	}
}