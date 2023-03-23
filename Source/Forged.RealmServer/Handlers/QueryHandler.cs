// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Realm;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Game.Common.Entities.Objects;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Query;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.QueryPlayerNames, Processing = PacketProcessing.Inplace)]
	void HandleQueryPlayerNames(QueryPlayerNames queryPlayerName)
	{
		QueryPlayerNamesResponse response = new();

		foreach (var guid in queryPlayerName.Players)
		{
			BuildNameQueryData(guid, out var nameCacheLookupResult);
			response.Players.Add(nameCacheLookupResult);
		}

		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QueryNpcText, Processing = PacketProcessing.Inplace)]
	void HandleNpcTextQuery(QueryNPCText packet)
	{
		var npcText = Global.ObjectMgr.GetNpcText(packet.TextID);

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
			Log.outError(LogFilter.Sql, "HandleNpcTextQuery: no BroadcastTextID found for text {0} in `npc_text table`", packet.TextID);

		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QueryCorpseLocationFromClient)]
	void HandleQueryCorpseLocation(QueryCorpseLocationFromClient queryCorpseLocation)
	{
		CorpseLocation packet = new();
		var player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseLocation.Player);

		if (!player || !player.HasCorpse || !_player.IsInSameRaidWith(player))
		{
			packet.Valid = false; // corpse not found
			packet.Player = queryCorpseLocation.Player;
			SendPacket(packet);

			return;
		}

		var corpseLocation = player.CorpseLocation;
		var corpseMapID = corpseLocation.MapId;
		var mapID = corpseLocation.MapId;
		var x = corpseLocation.X;
		var y = corpseLocation.Y;
		var z = corpseLocation.Z;

		// if corpse at different map
		if (mapID != player.Location.MapId)
		{
			// search entrance map for proper show entrance
			var corpseMapEntry = CliDB.MapStorage.LookupByKey(mapID);

			if (corpseMapEntry != null)
				if (corpseMapEntry.IsDungeon() && corpseMapEntry.CorpseMapID >= 0)
				{
					// if corpse map have entrance
					var entranceTerrain = Global.TerrainMgr.LoadTerrain((uint)corpseMapEntry.CorpseMapID);

					if (entranceTerrain != null)
					{
						mapID = (uint)corpseMapEntry.CorpseMapID;
						x = corpseMapEntry.Corpse.X;
						y = corpseMapEntry.Corpse.Y;
						z = entranceTerrain.GetStaticHeight(player.PhaseShift, mapID, x, y, MapConst.MaxHeight);
					}
				}
		}

		packet.Valid = true;
		packet.Player = queryCorpseLocation.Player;
		packet.MapID = (int)corpseMapID;
		packet.ActualMapID = (int)mapID;
		packet.Position = new Vector3(x, y, z);
		packet.Transport = ObjectGuid.Empty;
		SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.QueryCorpseTransport)]
	void HandleQueryCorpseTransport(QueryCorpseTransport queryCorpseTransport)
	{
		CorpseTransportQuery response = new();
		response.Player = queryCorpseTransport.Player;

		var player = Global.ObjAccessor.FindConnectedPlayer(queryCorpseTransport.Player);

		if (player)
		{
			var corpse = player.GetCorpse();

			if (_player.IsInSameRaidWith(player) && corpse && !corpse.GetTransGUID().IsEmpty && corpse.GetTransGUID() == queryCorpseTransport.Transport)
			{
				response.Position = new Vector3(corpse.TransOffsetX, corpse.TransOffsetY, corpse.TransOffsetZ);
				response.Facing = corpse.TransOffsetO;
			}
		}

		SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.ItemTextQuery, Processing = PacketProcessing.Inplace)]
	void HandleItemTextQuery(ItemTextQuery packet)
	{
		QueryItemTextResponse queryItemTextResponse = new();
		queryItemTextResponse.Id = packet.Id;

		var item = Player.GetItemByGuid(packet.Id);

		if (item)
		{
			queryItemTextResponse.Valid = true;
			queryItemTextResponse.Text = item.Text;
		}

		SendPacket(queryItemTextResponse);
	}

	[WorldPacketHandler(ClientOpcodes.QueryRealmName, Processing = PacketProcessing.Inplace)]
	void HandleQueryRealmName(QueryRealmName queryRealmName)
	{
		RealmQueryResponse realmQueryResponse = new();
		realmQueryResponse.VirtualRealmAddress = queryRealmName.VirtualRealmAddress;

		RealmId realmHandle = new(queryRealmName.VirtualRealmAddress);

		if (Global.ObjectMgr.GetRealmName(realmHandle.Index, ref realmQueryResponse.NameInfo.RealmNameActual, ref realmQueryResponse.NameInfo.RealmNameNormalized))
		{
			realmQueryResponse.LookupState = (byte)ResponseCodes.Success;
			realmQueryResponse.NameInfo.IsInternalRealm = false;
			realmQueryResponse.NameInfo.IsLocal = queryRealmName.VirtualRealmAddress == Global.WorldMgr.Realm.Id.GetAddress();
		}
		else
		{
			realmQueryResponse.LookupState = (byte)ResponseCodes.Failure;
		}
	}
}