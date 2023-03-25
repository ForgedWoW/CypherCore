// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class QueryNPCText : ClientPacket
{
	public ObjectGuid Guid;
	public uint TextID;
	public QueryNPCText(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TextID = _worldPacket.ReadUInt32();
		Guid = _worldPacket.ReadPackedGuid();
	}
}