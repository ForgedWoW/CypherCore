// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Party;

class PartyKillLog : ServerPacket
{
	public ObjectGuid Player;
	public ObjectGuid Victim;
	public PartyKillLog() : base(ServerOpcodes.PartyKillLog) { }

	public override void Write()
	{
		_worldPacket.WritePackedGuid(Player);
		_worldPacket.WritePackedGuid(Victim);
	}
}