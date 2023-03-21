// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Google.Protobuf;

namespace Forged.RealmServer;

public partial class WorldSession
{
	public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, IMessage response)
	{
		Response bnetResponse = new();
		bnetResponse.BnetStatus = BattlenetRpcErrorCode.Ok;
		bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
		bnetResponse.Method.ObjectId = 1;
		bnetResponse.Method.Token = token;

		if (response.CalculateSize() != 0)
			bnetResponse.Data.WriteBytes(response.ToByteArray());

		SendPacket(bnetResponse);
	}

	public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, BattlenetRpcErrorCode status)
	{
		Response bnetResponse = new();
		bnetResponse.BnetStatus = status;
		bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
		bnetResponse.Method.ObjectId = 1;
		bnetResponse.Method.Token = token;

		SendPacket(bnetResponse);
	}

	public void SendBattlenetRequest(uint serviceHash, uint methodId, IMessage request, Action<CodedInputStream> callback)
	{
		_battlenetResponseCallbacks[_battlenetRequestToken] = callback;
		SendBattlenetRequest(serviceHash, methodId, request);
	}

	public void SendBattlenetRequest(uint serviceHash, uint methodId, IMessage request)
	{
		Notification notification = new();
		notification.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
		notification.Method.ObjectId = 1;
		notification.Method.Token = _battlenetRequestToken++;

		if (request.CalculateSize() != 0)
			notification.Data.WriteBytes(request.ToByteArray());

		SendPacket(notification);
	}

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