// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Google.Protobuf;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Battlenet;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.ChangeRealmTicket, Status = SessionStatus.Authed)]
	void HandleBattlenetChangeRealmTicket(ChangeRealmTicket changeRealmTicket)
	{
		RealmListSecret = changeRealmTicket.Secret;

		ChangeRealmTicketResponse realmListTicket = new();
		realmListTicket.Token = changeRealmTicket.Token;
		realmListTicket.Allow = true;
		realmListTicket.Ticket = new Framework.IO.ByteBuffer();
		realmListTicket.Ticket.WriteCString("WorldserverRealmListTicket");

		SendPacket(realmListTicket);
	}
}