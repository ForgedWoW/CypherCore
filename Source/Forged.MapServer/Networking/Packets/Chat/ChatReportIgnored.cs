// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

class ChatReportIgnored : ClientPacket
{
	public ObjectGuid IgnoredGUID;
	public byte Reason;
	public ChatReportIgnored(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		IgnoredGUID = _worldPacket.ReadPackedGuid();
		Reason = _worldPacket.ReadUInt8();
	}
}