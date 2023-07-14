// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Battlenet;
using Forged.MapServer.Server;
using Forged.MapServer.Services;
using Framework.Constants;
using Game.Common.Handlers;
using Google.Protobuf;
using Serilog;

// ReSharper disable UnusedMember.Local
// ReSharper disable CollectionNeverQueried.Local

namespace Forged.MapServer.OpCodeHandlers;

public class BattlenetHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly WorldServiceManager _serviceManager;
    private readonly Dictionary<uint, Action<CodedInputStream>> _battlenetResponseCallbacks = new();
    private uint _battlenetRequestToken;

    public BattlenetHandler(WorldSession session, WorldServiceManager serviceManager)
    {
        _session = session;
        _serviceManager = serviceManager;
    }

    public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, IMessage response)
	{
		Response bnetResponse = new()
        {
            BnetStatus = BattlenetRpcErrorCode.Ok
        };

        bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
		bnetResponse.Method.ObjectId = 1;
		bnetResponse.Method.Token = token;

		if (response.CalculateSize() != 0)
			bnetResponse.Data.WriteBytes(response.ToByteArray());

		_session.SendPacket(bnetResponse);
	}

	public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, BattlenetRpcErrorCode status)
	{
		Response bnetResponse = new()
        {
            BnetStatus = status
        };

        bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
		bnetResponse.Method.ObjectId = 1;
		bnetResponse.Method.Token = token;

		_session.SendPacket(bnetResponse);
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

		_session.SendPacket(notification);
	}

	[WorldPacketHandler(ClientOpcodes.BattlenetRequest, Status = SessionStatus.Authed)]
	void HandleBattlenetRequest(BattlenetRequest request)
	{
		var handler = _serviceManager.GetHandler(request.Method.GetServiceHash(), request.Method.GetMethodId());

		if (handler != null)
		{
			handler.Invoke(_session, request.Method, new CodedInputStream(request.Data));
		}
		else
		{
			SendBattlenetResponse(request.Method.GetServiceHash(), request.Method.GetMethodId(), request.Method.Token, BattlenetRpcErrorCode.RpcNotImplemented);
			Log.Logger.Debug( "{0} tried to call invalid service {1}", _session.GetPlayerInfo(), request.Method.GetServiceHash());
		}
	}

	[WorldPacketHandler(ClientOpcodes.ChangeRealmTicket, Status = SessionStatus.Authed)]
	void HandleBattlenetChangeRealmTicket(ChangeRealmTicket changeRealmTicket)
	{
		_session.RealmListSecret = changeRealmTicket.Secret;

		ChangeRealmTicketResponse realmListTicket = new()
        {
            Token = changeRealmTicket.Token,
            Allow = true,
            Ticket = new Framework.IO.ByteBuffer()
        };

        realmListTicket.Ticket.WriteCString("WorldserverRealmListTicket");

		_session.SendPacket(realmListTicket);
	}
}