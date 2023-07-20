// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Accounts;
using Forged.MapServer.Cache;
using Forged.MapServer.Chrono;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Globals;
using Forged.MapServer.Maps;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Query;
using Forged.MapServer.Server;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Realm;
using Game.Common.Handlers;
using Microsoft.Extensions.Configuration;
using Serilog;
using System.Collections.Generic;
using System.Data;
using System.Numerics;

namespace Forged.MapServer.OpCodeHandlers;

public class QueryHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly ObjectAccessor _objectAccessor;
	private readonly GameObjectManager _gameObjectManager;
    private readonly IConfiguration _config;
    private readonly TerrainManager _terrainManager;
    private readonly CliDB _cliDb;
	private readonly CharacterCache _characterCache;
	private readonly BNetAccountManager _bNetAccountManager;

    public QueryHandler(WorldSession session, ObjectAccessor objectAccessor, GameObjectManager gameObjectManager,
        IConfiguration config, TerrainManager terrainManager, CliDB cliDb, CharacterCache characterCache,
        BNetAccountManager bNetAccountManager)
    {
		_session = session;
		_objectAccessor = objectAccessor;
		_gameObjectManager = gameObjectManager;
		_config = config;
		_terrainManager = terrainManager;
		_cliDb = cliDb;
		_characterCache = characterCache;
		_bNetAccountManager = bNetAccountManager;
    }

	public void BuildNameQueryData(ObjectGuid guid, out NameCacheLookupResult lookupData)
	{
		lookupData = new NameCacheLookupResult();

		var player = _objectAccessor.FindPlayer(guid);

		lookupData.Player = guid;

		lookupData.Data = new PlayerGuidLookupData();

		if (lookupData.Data.Initialize(guid, _characterCache, _bNetAccountManager, player))
			lookupData.Result = (byte)ResponseCodes.Success;
		else
			lookupData.Result = (byte)ResponseCodes.Failure; // name unknown
	}

	[WorldPacketHandler(ClientOpcodes.QueryPlayerNames, Processing = PacketProcessing.Inplace)]
	void HandleQueryPlayerNames(QueryPlayerNames queryPlayerName)
	{
		QueryPlayerNamesResponse response = new();

		foreach (var guid in queryPlayerName.Players)
		{
			BuildNameQueryData(guid, out var nameCacheLookupResult);
			response.Players.Add(nameCacheLookupResult);
		}

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QueryTime, Processing = PacketProcessing.Inplace)]
	void HandleQueryTime(QueryTime packet)
	{
		SendQueryTimeResponse();
	}

	void SendQueryTimeResponse()
	{
		QueryTimeResponse queryTimeResponse = new();
		queryTimeResponse.CurrentTime = GameTime.CurrentTime;
		_session.SendPacket(queryTimeResponse);
	}

	[WorldPacketHandler(ClientOpcodes.QueryGameObject, Processing = PacketProcessing.Inplace)]
	void HandleGameObjectQuery(QueryGameObject packet)
	{
		var info = _gameObjectManager.GetGameObjectTemplate(packet.GameObjectID);

		if (info != null)
		{
			if (!_config.GetValue("CacheDataQueries", true))
				info.InitializeQueryData(_gameObjectManager);

			var queryGameObjectResponse = info.QueryData;

			var loc = _session.SessionDbLocaleIndex;

			if (loc != Locale.enUS)
			{
				var gameObjectLocale = _gameObjectManager.GetGameObjectLocale(queryGameObjectResponse.GameObjectID);

				if (gameObjectLocale != null)
				{
                    _gameObjectManager.GetLocaleString(gameObjectLocale.Name, loc, ref queryGameObjectResponse.Stats.Name[0]);
                    _gameObjectManager.GetLocaleString(gameObjectLocale.CastBarCaption, loc, ref queryGameObjectResponse.Stats.CastBarCaption);
                    _gameObjectManager.GetLocaleString(gameObjectLocale.Unk1, loc, ref queryGameObjectResponse.Stats.UnkString);
				}
			}

			_session.SendPacket(queryGameObjectResponse);
		}
		else
		{
			Log.Logger.Debug($"WORLD: CMSG_GAMEOBJECT_QUERY - Missing gameobject info for (ENTRY: {packet.GameObjectID})");

			QueryGameObjectResponse response = new();
			response.GameObjectID = packet.GameObjectID;
			response.Guid = packet.Guid;
			_session.SendPacket(response);
		}
	}

	[WorldPacketHandler(ClientOpcodes.QueryCreature, Processing = PacketProcessing.Inplace)]
	void HandleCreatureQuery(QueryCreature packet)
	{
		var ci = _gameObjectManager.GetCreatureTemplate(packet.CreatureID);

		if (ci != null)
		{
			if (!_config.GetValue("CacheDataQueries", true))
				ci.InitializeQueryData();

			var queryCreatureResponse = ci.QueryData;

			var loc = _session.SessionDbLocaleIndex;

			if (loc != Locale.enUS)
			{
				var creatureLocale = _gameObjectManager.GetCreatureLocale(ci.Entry);

				if (creatureLocale != null)
				{
					var name = queryCreatureResponse.Stats.Name[0];
					var nameAlt = queryCreatureResponse.Stats.NameAlt[0];

					_gameObjectManager.GetLocaleString(creatureLocale.Name, loc, ref name);
					_gameObjectManager.GetLocaleString(creatureLocale.NameAlt, loc, ref nameAlt);
					_gameObjectManager.GetLocaleString(creatureLocale.Title, loc, ref queryCreatureResponse.Stats.Title);
					_gameObjectManager.GetLocaleString(creatureLocale.TitleAlt, loc, ref queryCreatureResponse.Stats.TitleAlt);

					queryCreatureResponse.Stats.Name[0] = name;
					queryCreatureResponse.Stats.NameAlt[0] = nameAlt;
				}
			}

			_session.SendPacket(queryCreatureResponse);
		}
		else
		{
			Log.Logger.Debug($"WORLD: CMSG_QUERY_CREATURE - NO CREATURE INFO! (ENTRY: {packet.CreatureID})");

			QueryCreatureResponse response = new();
			response.CreatureID = packet.CreatureID;
			_session.SendPacket(response);
		}
	}

	[WorldPacketHandler(ClientOpcodes.QueryNpcText, Processing = PacketProcessing.Inplace)]
	void HandleNpcTextQuery(QueryNPCText packet)
	{
		var npcText = _gameObjectManager.GetNpcText(packet.TextID);

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

	[WorldPacketHandler(ClientOpcodes.QueryPageText, Processing = PacketProcessing.Inplace)]
	void HandleQueryPageText(QueryPageText packet)
	{
		QueryPageTextResponse response = new();
		response.PageTextID = packet.PageTextID;

		var pageID = packet.PageTextID;

		while (pageID != 0)
		{
			var pageText = _gameObjectManager.GetPageText(pageID);

			if (pageText == null)
				break;

			QueryPageTextResponse.PageTextInfo page;
			page.Id = pageID;
			page.NextPageID = pageText.NextPageID;
			page.Text = pageText.Text;
			page.PlayerConditionID = pageText.PlayerConditionID;
			page.Flags = pageText.Flags;

			var locale = _session.SessionDbLocaleIndex;

			if (locale != Locale.enUS)
			{
				var pageLocale = _gameObjectManager.GetPageTextLocale(pageID);

				if (pageLocale != null)
					_gameObjectManager.GetLocaleString(pageLocale.Text, locale, ref page.Text);
			}

			response.Pages.Add(page);
			pageID = pageText.NextPageID;
		}

		response.Allow = !response.Pages.Empty();
		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QueryCorpseLocationFromClient)]
	void HandleQueryCorpseLocation(QueryCorpseLocationFromClient queryCorpseLocation)
	{
		CorpseLocation packet = new();
		var player = _objectAccessor.FindConnectedPlayer(queryCorpseLocation.Player);

		if (player == null || !player.HasCorpse || !_session.Player.IsInSameRaidWith(player))
		{
			packet.Valid = false; // corpse not found
			packet.Player = queryCorpseLocation.Player;
			_session.SendPacket(packet);

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
			var corpseMapEntry = _cliDb.MapStorage.LookupByKey(mapID);

			if (corpseMapEntry != null)
				if (corpseMapEntry.IsDungeon() && corpseMapEntry.CorpseMapID >= 0)
				{
					// if corpse map have entrance
					var entranceTerrain = _terrainManager.LoadTerrain((uint)corpseMapEntry.CorpseMapID);

					if (entranceTerrain != null)
					{
						mapID = (uint)corpseMapEntry.CorpseMapID;
						x = corpseMapEntry.Corpse.X;
						y = corpseMapEntry.Corpse.Y;
						z = entranceTerrain.GetStaticHeight(player.Location.PhaseShift, mapID, x, y, MapConst.MaxHeight);
					}
				}
		}

		packet.Valid = true;
		packet.Player = queryCorpseLocation.Player;
		packet.MapID = (int)corpseMapID;
		packet.ActualMapID = (int)mapID;
		packet.Position = new Vector3(x, y, z);
		packet.Transport = ObjectGuid.Empty;
		_session.SendPacket(packet);
	}

	[WorldPacketHandler(ClientOpcodes.QueryCorpseTransport)]
	void HandleQueryCorpseTransport(QueryCorpseTransport queryCorpseTransport)
	{
		CorpseTransportQuery response = new();
		response.Player = queryCorpseTransport.Player;

		var player = _objectAccessor.FindConnectedPlayer(queryCorpseTransport.Player);

		if (player != null)
		{
			var corpse = player.Corpse;

			if (_session.Player.IsInSameRaidWith(player) && corpse != null && !corpse.GetTransGUID().IsEmpty && corpse.GetTransGUID() == queryCorpseTransport.Transport)
            {
				response.Position = new Vector3(corpse.MovementInfo.Transport.Pos.X, corpse.MovementInfo.Transport.Pos.Y, corpse.MovementInfo.Transport.Pos.Z);
				response.Facing = corpse.MovementInfo.Transport.Pos.Orientation;
			}
		}

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QueryQuestCompletionNpcs, Processing = PacketProcessing.Inplace)]
	void HandleQueryQuestCompletionNPCs(QueryQuestCompletionNPCs queryQuestCompletionNPCs)
	{
		QuestCompletionNPCResponse response = new();

		foreach (var questID in queryQuestCompletionNPCs.QuestCompletionNPCs)
		{
			QuestCompletionNPC questCompletionNPC = new();

			if (_gameObjectManager.GetQuestTemplate(questID) == null)
			{
				Log.Logger.Debug("WORLD: Unknown quest {0} in CMSG_QUEST_NPC_QUERY by {1}", questID, _session.Player.GUID);

				continue;
			}

			questCompletionNPC.QuestID = questID;

			foreach (var id in _gameObjectManager.GetCreatureQuestInvolvedRelationReverseBounds(questID))
				questCompletionNPC.NPCs.Add(id);

			foreach (var id in _gameObjectManager.GetGOQuestInvolvedRelationReverseBounds(questID))
				questCompletionNPC.NPCs.Add(id | 0x80000000); // GO mask

			response.QuestCompletionNPCs.Add(questCompletionNPC);
		}

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.QuestPoiQuery, Processing = PacketProcessing.Inplace)]
	void HandleQuestPOIQuery(QuestPOIQuery packet)
	{
		if (packet.MissingQuestCount >= SharedConst.MaxQuestLogSize)
			return;

		// Read quest ids and add the in a unordered_set so we don't send POIs for the same quest multiple times
		HashSet<uint> questIds = new();

		for (var i = 0; i < packet.MissingQuestCount; ++i)
			questIds.Add(packet.MissingQuestPOIs[i]); // QuestID

		QuestPOIQueryResponse response = new(_config);

		foreach (var questId in questIds)
			if (_session.Player.FindQuestSlot(questId) != SharedConst.MaxQuestLogSize)
			{
				var poiData = _gameObjectManager.GetQuestPOIData(questId);

				if (poiData != null)
					response.QuestPOIDataStats.Add(poiData);
			}

		_session.SendPacket(response);
	}

	[WorldPacketHandler(ClientOpcodes.ItemTextQuery, Processing = PacketProcessing.Inplace)]
	void HandleItemTextQuery(ItemTextQuery packet)
	{
		QueryItemTextResponse queryItemTextResponse = new();
		queryItemTextResponse.Id = packet.Id;

		var item = _session.Player.GetItemByGuid(packet.Id);

		if (item != null)
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

		if (_gameObjectManager.GetRealmName(realmHandle.Index, ref realmQueryResponse.NameInfo.RealmNameActual, ref realmQueryResponse.NameInfo.RealmNameNormalized))
		{
			realmQueryResponse.LookupState = (byte)ResponseCodes.Success;
			realmQueryResponse.NameInfo.IsInternalRealm = false;
			realmQueryResponse.NameInfo.IsLocal = queryRealmName.VirtualRealmAddress == WorldManager.Realm.Id.VirtualRealmAddress;
		}
		else
		{
			realmQueryResponse.LookupState = (byte)ResponseCodes.Failure;
		}
	}
}