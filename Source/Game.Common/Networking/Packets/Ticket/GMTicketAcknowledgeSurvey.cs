// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Networking;

// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Networking.Packets.Ticket;

public class GMTicketAcknowledgeSurvey : ClientPacket
{
	int CaseID;
	public GMTicketAcknowledgeSurvey(WorldPacket packet) : base(packet) { }

	public override void Read()
	{
		CaseID = _worldPacket.ReadInt32();
	}
}
