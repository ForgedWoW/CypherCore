// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Numerics;
using Framework.Constants;
using Framework.Realm;
using Game.DataStorage;
using Game.Entities;
using Game.Networking;
using Game.Networking.Packets;

namespace Game;

public partial class WorldSession
{
	public void BuildNameQueryData(ObjectGuid guid, out NameCacheLookupResult lookupData)
	{
		lookupData = new NameCacheLookupResult();

		var player = Global.ObjAccessor.FindPlayer(guid);

		lookupData.Player = guid;

		lookupData.Data = new PlayerGuidLookupData();

		if (lookupData.Data.Initialize(guid, player))
			lookupData.Result = (byte)ResponseCodes.Success;
		else
			lookupData.Result = (byte)ResponseCodes.Failure; // name unknown
	}

	[WorldPacketHandler(ClientOpcodes.QueryTime, Processing = PacketProcessing.Inplace)]
	void HandleQueryTime(QueryTime packet)
	{
		SendQueryTimeResponse();
	}

	void SendQueryTimeResponse()
	{
		QueryTimeResponse queryTimeResponse = new();
		queryTimeResponse.CurrentTime = GameTime.GetGameTime();
		SendPacket(queryTimeResponse);
	}

	[WorldPacketHandler(ClientOpcodes.QueryGameObject, Processing = PacketProcessing.Inplace)]
	void HandleGameObjectQuery(QueryGameObject packet)
	{
		var info = Global.ObjectMgr.GetGameObjectTemplate(packet.GameObjectID);

		if (info != null)
		{
			if (!WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries))
				info.InitializeQueryData();

			var queryGameObjectResponse = info.QueryData;

			var loc = SessionDbLocaleIndex;

			if (loc != Locale.enUS)
			{
				var gameObjectLocale = Global.ObjectMgr.GetGameObjectLocale(queryGameObjectResponse.GameObjectID);

				if (gameObjectLocale != null)
				{
					ObjectManager.GetLocaleString(gameObjectLocale.Name, loc, ref queryGameObjectResponse.Stats.Name[0]);
					ObjectManager.GetLocaleString(gameObjectLocale.CastBarCaption, loc, ref queryGameObjectResponse.Stats.CastBarCaption);
					ObjectManager.GetLocaleString(gameObjectLocale.Unk1, loc, ref queryGameObjectResponse.Stats.UnkString);
				}
			}

			SendPacket(queryGameObjectResponse);
		}
		else
		{
			Log.outDebug(LogFilter.Network, $"WORLD: CMSG_GAMEOBJECT_QUERY - Missing gameobject info for (ENTRY: {packet.GameObjectID})");

			QueryGameObjectResponse response = new();
			response.GameObjectID = packet.GameObjectID;
			response.Guid = packet.Guid;
			SendPacket(response);
		}
	}

	[WorldPacketHandler(ClientOpcodes.QueryCreature, Processing = PacketProcessing.Inplace)]
	void HandleCreatureQuery(QueryCreature packet)
	{
		var ci = Global.ObjectMgr.GetCreatureTemplate(packet.CreatureID);

		if (ci != null)
		{
			if (!WorldConfig.GetBoolValue(WorldCfg.CacheDataQueries))
				ci.InitializeQueryData();

			var queryCreatureResponse = ci.QueryData;

			var loc = SessionDbLocaleIndex;

			if (loc != Locale.enUS)
			{
				var creatureLocale = Global.ObjectMgr.GetCreatureLocale(ci.Entry);

				if (creatureLocale != null)
				{
					var name = queryCreatureResponse.Stats.Name[0];
					var nameAlt = queryCreatureResponse.Stats.NameAlt[0];

					ObjectManager.GetLocaleString(creatureLocale.Name, loc, ref name);
					ObjectManager.GetLocaleString(creatureLocale.NameAlt, loc, ref nameAlt);
					ObjectManager.GetLocaleString(creatureLocale.Title, loc, ref queryCreatureResponse.Stats.Title);
					ObjectManager.GetLocaleString(creatureLocale.TitleAlt, loc, ref queryCreatureResponse.Stats.TitleAlt);

					queryCreatureResponse.Stats.Name[0] = name;
					queryCreatureResponse.Stats.NameAlt[0] = nameAlt;
				}
			}

			SendPacket(queryCreatureResponse);
		}
		else
		{
			Log.outDebug(LogFilter.Network, $"WORLD: CMSG_QUERY_CREATURE - NO CREATURE INFO! (ENTRY: {packet.CreatureID})");

			QueryCreatureResponse response = new();
			response.CreatureID = packet.CreatureID;
			SendPacket(response);
		}
	}

	[WorldPacketHandler(ClientOpcodes.QueryPageText, Processing = PacketProcessing.Inplace)]
	void HandleQueryPageText(QueryPageText packet)
	{
		QueryPageTextResponse response = new();
		response.PageTextID = packet.PageTextID;

		var pageID = packet.PageTextID;

		while (pageID != 0)
		{
			var pageText = Global.ObjectMgr.GetPageText(pageID);

			if (pageText == null)
				break;

			QueryPageTextResponse.PageTextInfo page;
			page.Id = pageID;
			page.NextPageID = pageText.NextPageID;
			page.Text = pageText.Text;
			page.PlayerConditionID = pageText.PlayerConditionID;
			page.Flags = pageText.Flags;

			var locale = SessionDbLocaleIndex;

			if (locale != Locale.enUS)
			{
				var pageLocale = Global.ObjectMgr.GetPageTextLocale(pageID);

				if (pageLocale != null)
					ObjectManager.GetLocaleString(pageLocale.Text, locale, ref page.Text);
			}

			response.Pages.Add(page);
			pageID = pageText.NextPageID;
		}

		response.Allow = !response.Pages.Empty();
		SendPacket(response);
	}


	[WorldPacketHandler(ClientOpcodes.QueryQuestCompletionNpcs, Processing = PacketProcessing.Inplace)]
	void HandleQueryQuestCompletionNPCs(QueryQuestCompletionNPCs queryQuestCompletionNPCs)
	{
		QuestCompletionNPCResponse response = new();

		foreach (var questID in queryQuestCompletionNPCs.QuestCompletionNPCs)
		{
			QuestCompletionNPC questCompletionNPC = new();

			if (Global.ObjectMgr.GetQuestTemplate(questID) == null)
			{
				Log.outDebug(LogFilter.Network, "WORLD: Unknown quest {0} in CMSG_QUEST_NPC_QUERY by {1}", questID, Player.GUID);

				continue;
			}

			questCompletionNPC.QuestID = questID;

			foreach (var id in Global.ObjectMgr.GetCreatureQuestInvolvedRelationReverseBounds(questID))
				questCompletionNPC.NPCs.Add(id);

			foreach (var id in Global.ObjectMgr.GetGOQuestInvolvedRelationReverseBounds(questID))
				questCompletionNPC.NPCs.Add(id | 0x80000000); // GO mask

			response.QuestCompletionNPCs.Add(questCompletionNPC);
		}

		SendPacket(response);
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

		QuestPOIQueryResponse response = new();

		foreach (var questId in questIds)
			if (_player.FindQuestSlot(questId) != SharedConst.MaxQuestLogSize)
			{
				var poiData = Global.ObjectMgr.GetQuestPOIData(questId);

				if (poiData != null)
					response.QuestPOIDataStats.Add(poiData);
			}

		SendPacket(response);
	}

}