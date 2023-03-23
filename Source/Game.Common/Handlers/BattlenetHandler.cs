// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Battlenet;
using Game.Common.Server;
using Game.Common.Services;
using Google.Protobuf;

namespace Game.Common.Handlers;

public class BattlenetHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly WorldServiceManager _worldServiceManager;
    readonly Dictionary<uint, Action<Google.Protobuf.CodedInputStream>> _battlenetResponseCallbacks = new();
    uint _battlenetRequestToken;

    public BattlenetHandler(WorldSession session, WorldServiceManager worldServiceManager)
    {
        _session = session;
        _worldServiceManager = worldServiceManager;
    }

    public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, IMessage response)
	{
		Response bnetResponse = new();
		bnetResponse.BnetStatus = BattlenetRpcErrorCode.Ok;
		bnetResponse.Method.Type = MathFunctions.MakePair64(methodId, serviceHash);
		bnetResponse.Method.ObjectId = 1;
		bnetResponse.Method.Token = token;

		if (response.CalculateSize() != 0)
			bnetResponse.Data.WriteBytes(response.ToByteArray());

        _session.SendPacket(bnetResponse);
	}

	public void SendBattlenetResponse(uint serviceHash, uint methodId, uint token, BattlenetRpcErrorCode status)
	{
		Response bnetResponse = new();
		bnetResponse.BnetStatus = status;
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
		var handler = _worldServiceManager.GetHandler(request.Method.GetServiceHash(), request.Method.GetMethodId());

		if (handler != null)
		{
			handler.Invoke(_session, request.Method, new CodedInputStream(request.Data));
		}
		else
		{
			SendBattlenetResponse(request.Method.GetServiceHash(), request.Method.GetMethodId(), request.Method.Token, BattlenetRpcErrorCode.RpcNotImplemented);
			Log.outDebug(LogFilter.SessionRpc, "{0} tried to call invalid service {1}", _session.GetPlayerInfo(), request.Method.GetServiceHash());
		}
	}
}
