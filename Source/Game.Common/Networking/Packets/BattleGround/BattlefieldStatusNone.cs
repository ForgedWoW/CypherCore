// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;

namespace Game.Networking.Packets;

public class BattlefieldStatusNone : ServerPacket
{
	public RideTicket Ticket = new();
	public BattlefieldStatusNone() : base(ServerOpcodes.BattlefieldStatusNone) { }

	public override void Write()
	{
		Ticket.Write(_worldPacket);
	}
}