// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Networking.Packets;

public class SetPartyAssignment : ClientPacket
{
	public byte Assignment;
	public byte PartyIndex;
	public ObjectGuid Target;
	public bool Set;
	public SetPartyAssignment(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();
		Assignment = _worldPacket.ReadUInt8();
		Target = _worldPacket.ReadPackedGuid();
		Set = _worldPacket.HasBit();
	}
}