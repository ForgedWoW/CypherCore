// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Realm;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Game.Common.Handlers;
using Forged.RealmServer.Networking.Packets;
using Serilog;
using Forged.RealmServer.Globals;

namespace Forged.RealmServer;

public class QueryHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly GameObjectManager _objectManager;
    private readonly ObjectAccessor _objectAccessor;
    private readonly CliDB _cliDB;
    private readonly WorldManager _worldManager;

    public QueryHandler(WorldSession session, GameObjectManager objectManager, ObjectAccessor objectAccessor,
		CliDB cliDB, WorldManager worldManager)
    {
        _session = session;
        _objectManager = objectManager;
        _objectAccessor = objectAccessor;
        _cliDB = cliDB;
        _worldManager = worldManager;
    }

    [WorldPacketHandler(ClientOpcodes.QueryPlayerNames, Processing = PacketProcessing.Inplace)]
	void HandleQueryPlayerNames(QueryPlayerNames queryPlayerName)
	{
		QueryPlayerNamesResponse response = new();

		foreach (var guid in queryPlayerName.Players)
		{
			_session.BuildNameQueryData(guid, out var nameCacheLookupResult);
			response.Players.Add(nameCacheLookupResult);
		}

		_session.SendPacket(response);
	}
	
	[WorldPacketHandler(ClientOpcodes.QueryNpcText, Processing = PacketProcessing.Inplace)]
	void HandleNpcTextQuery(QueryNPCText packet)
	{
		var npcText = _objectManager.GetNpcText(packet.TextID);

		QueryNPCTextResponse response = new();
		response.TextID = packet.TextID;

		if (npcText != null)
			for (byte i = 0; i < SharedConst.MaxNpcTextOptions; ++i)
			{
				response.Probabilities[i] = npcText.Data[i].Probability;
				response.BroadcastTextID[i] = npcText.Data[i].BroadcastTextID;

				if (!response.Allow && npcText.Data[i].BroadcastTextID != 0)
					response.Allow = true;
			}

		if (!response.Allow)
			Log.Logger.Error("HandleNpcTextQuery: no BroadcastTextID found for text {0} in `npc_text table`", packet.TextID);

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QueryCorpseLocationFromClient)]
	void HandleQueryCorpseLocation(QueryCorpseLocationFromClient queryCorpseLocation)
    {
        // Send to map server
    }

    [WorldPacketHandler(ClientOpcodes.QueryCorpseTransport)]
	void HandleQueryCorpseTransport(QueryCorpseTransport queryCorpseTransport)
	{
		CorpseTransportQuery response = new();
		response.Player = queryCorpseTransport.Player;

		var player = _objectAccessor.FindConnectedPlayer(queryCorpseTransport.Player);

		if (player)
		{
			var corpse = player.GetCorpse();

			if (_session.Player.IsInSameRaidWith(player) && corpse && !corpse.GetTransGUID().IsEmpty && corpse.GetTransGUID() == queryCorpseTransport.Transport)
			{
				response.Position = new Vector3(corpse.TransOffsetX, corpse.TransOffsetY, corpse.TransOffsetZ);
				response.Facing = corpse.TransOffsetO;
			}
		}

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.ItemTextQuery, Processing = PacketProcessing.Inplace)]
	void HandleItemTextQuery(ItemTextQuery packet)
	{
		QueryItemTextResponse queryItemTextResponse = new();
		queryItemTextResponse.Id = packet.Id;

		var item = _session.Player.GetItemByGuid(packet.Id);

		if (item)
		{
			queryItemTextResponse.Valid = true;
			queryItemTextResponse.Text = item.Text;
		}

		_session.SendPacket(queryItemTextResponse);
	}

	[WorldPacketHandler(ClientOpcodes.QueryRealmName, Processing = PacketProcessing.Inplace)]
	void HandleQueryRealmName(QueryRealmName queryRealmName)
	{
		RealmQueryResponse realmQueryResponse = new();
		realmQueryResponse.VirtualRealmAddress = queryRealmName.VirtualRealmAddress;

		RealmId realmHandle = new(queryRealmName.VirtualRealmAddress);

		if (_objectManager.GetRealmName(realmHandle.Index, ref realmQueryResponse.NameInfo.RealmNameActual, ref realmQueryResponse.NameInfo.RealmNameNormalized))
		{
			realmQueryResponse.LookupState = (byte)ResponseCodes.Success;
			realmQueryResponse.NameInfo.IsInternalRealm = false;
			realmQueryResponse.NameInfo.IsLocal = queryRealmName.VirtualRealmAddress == _worldManager.Realm.Id.GetAddress();
		}
		else
		{
			realmQueryResponse.LookupState = (byte)ResponseCodes.Failure;
		}
	}
}