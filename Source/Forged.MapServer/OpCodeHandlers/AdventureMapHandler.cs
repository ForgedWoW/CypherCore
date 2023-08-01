// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.AdventureMap;
using Forged.MapServer.Server;
using Framework.Constants;
using Game.Common.Handlers;
// ReSharper disable UnusedMember.Local

namespace Forged.MapServer.OpCodeHandlers;

public class AdventureMapHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly GameObjectManager _objectManager;
    private readonly DB6Storage<AdventureMapPOIRecord> _adventureMapPOIRecords;

    public AdventureMapHandler(WorldSession session, GameObjectManager objectManager, DB6Storage<AdventureMapPOIRecord> adventureMapPOIRecords)
    {
        _session = session;
        _objectManager = objectManager;
        _adventureMapPOIRecords = adventureMapPOIRecords;
    }

    [WorldPacketHandler(ClientOpcodes.AdventureMapStartQuest)]
    private void HandleAdventureMapStartQuest(AdventureMapStartQuest startQuest)
    {
        var quest = _objectManager.QuestTemplateCache.GetQuestTemplate(startQuest.QuestID);

        if (quest == null)
            return;

        var adventureMapPOI = _adventureMapPOIRecords.Values.FirstOrDefault(adventureMap => adventureMap.QuestID == startQuest.QuestID && _session.Player.MeetPlayerCondition(adventureMap.PlayerConditionID));

        if (adventureMapPOI == null)
            return;

        if (_session.Player.CanTakeQuest(quest, true))
            _session.Player.AddQuestAndCheckCompletion(quest, _session.Player);
    }
}