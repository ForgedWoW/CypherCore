// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.


// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Party;

public class PartyInviteResponse : ClientPacket
{
	public byte PartyIndex;
	public bool Accept;
	public uint? RolesDesired;
	public PartyInviteResponse(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadUInt8();

		Accept = _worldPacket.HasBit();

		var hasRolesDesired = _worldPacket.HasBit();

		if (hasRolesDesired)
			RolesDesired = _worldPacket.ReadUInt32();
	}
}
