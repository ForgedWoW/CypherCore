// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Common.Globals;
using Game.Common.Networking;
using Game.Common.Networking.Packets.Query;
using Game.Common.Server;

namespace Game.Common.Handlers;

public class QueryHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;

    public QueryHandler(WorldSession session)
    {
        _session = session;
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

			var loc = _session.SessionDbLocaleIndex;

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

            _session.SendPacket(queryGameObjectResponse);
		}
		else
		{
			Log.outDebug(LogFilter.Network, $"WORLD: CMSG_GAMEOBJECT_QUERY - Missing gameobject info for (ENTRY: {packet.GameObjectID})");

			QueryGameObjectResponse response = new();
			response.GameObjectID = packet.GameObjectID;
			response.Guid = packet.Guid;
            _session.SendPacket(response);
		}
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
			if (_session.Player.FindQuestSlot(questId) != SharedConst.MaxQuestLogSize)
			{
				var poiData = Global.ObjectMgr.GetQuestPOIData(questId);

				if (poiData != null)
					response.QuestPOIDataStats.Add(poiData);
			}

        _session.SendPacket(response);
	}

}
