// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Party;

public class RequestPartyJoinUpdates : ClientPacket
{
	public sbyte PartyIndex;
	public RequestPartyJoinUpdates(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		PartyIndex = _worldPacket.ReadInt8();
	}
}
