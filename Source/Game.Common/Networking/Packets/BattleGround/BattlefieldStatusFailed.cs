﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;

namespace Game.Networking.Packets;

public class BattlefieldStatusFailed : ServerPacket
{
	public ulong QueueID;
	public ObjectGuid ClientID;
	public int Reason;
	public RideTicket Ticket = new();
	public BattlefieldStatusFailed() : base(ServerOpcodes.BattlefieldStatusFailed) { }

	public override void Write()
	{
		Ticket.Write(_worldPacket);
		_worldPacket.WriteUInt64(QueueID);
		_worldPacket.WriteInt32(Reason);
		_worldPacket.WritePackedGuid(ClientID);
	}
}