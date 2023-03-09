// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Networking.Packets;

class BattlemasterJoinArena : ClientPacket
{
	public byte TeamSizeIndex;
	public byte Roles;
	public BattlemasterJoinArena(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TeamSizeIndex = _worldPacket.ReadUInt8();
		Roles = _worldPacket.ReadUInt8();
	}
}