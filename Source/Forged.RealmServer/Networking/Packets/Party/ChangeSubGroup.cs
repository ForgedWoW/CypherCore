// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;

namespace Forged.RealmServer.Networking.Packets;

class ChangeSubGroup : ClientPacket
{
	public ObjectGuid TargetGUID;
	public sbyte PartyIndex;
	public byte NewSubGroup;
	public ChangeSubGroup(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		TargetGUID = _worldPacket.ReadPackedGuid();
		PartyIndex = _worldPacket.ReadInt8();
		NewSubGroup = _worldPacket.ReadUInt8();
	}
}