// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

public class GossipSelectOption : ClientPacket
{
	public ObjectGuid GossipUnit;
	public int GossipOptionID;
	public uint GossipID;
	public string PromotionCode;
	public GossipSelectOption(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		GossipUnit = _worldPacket.ReadPackedGuid();
		GossipID = _worldPacket.ReadUInt32();
		GossipOptionID = _worldPacket.ReadInt32();

		var length = _worldPacket.ReadBits<uint>(8);
		PromotionCode = _worldPacket.ReadString(length);
	}
}