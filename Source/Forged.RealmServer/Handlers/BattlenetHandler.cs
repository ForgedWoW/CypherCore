// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Battlenet;
using Game.Common.Handlers;
using Game.Common.Server;

namespace Forged.RealmServer;

public class BattlenetHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public BattlenetHandler(WorldSession session)
    {
        _session = session;
    }

    [WorldPacketHandler(ClientOpcodes.ChangeRealmTicket, Status = SessionStatus.Authed)]
	void HandleBattlenetChangeRealmTicket(ChangeRealmTicket changeRealmTicket)
	{
        _session.RealmListSecret = changeRealmTicket.Secret;

		ChangeRealmTicketResponse realmListTicket = new();
		realmListTicket.Token = changeRealmTicket.Token;
		realmListTicket.Allow = true;
		realmListTicket.Ticket = new Framework.IO.ByteBuffer();
		realmListTicket.Ticket.WriteCString("WorldserverRealmListTicket");

		_session.SendPacket(realmListTicket);
	}
}