// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Networking.Packets.LFG;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.BattleGround;

public class BattlefieldStatusNone : ServerPacket
{
	public RideTicket Ticket = new();
	public BattlefieldStatusNone() : base(ServerOpcodes.BattlefieldStatusNone) { }

	public override void Write()
	{
		Ticket.Write(_worldPacket);
	}
}