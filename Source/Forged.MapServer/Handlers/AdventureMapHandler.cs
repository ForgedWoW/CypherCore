// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Linq;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.AdventureMap;
using Framework.Constants;
using Game.Common.Handlers;

namespace Forged.MapServer.Handlers;

public class AdventureMapHandler : IWorldSessionHandler
{
	[WorldPacketHandler(ClientOpcodes.AdventureMapStartQuest)]
    private void HandleAdventureMapStartQuest(AdventureMapStartQuest startQuest)
	{
		var quest = Global.ObjectMgr.GetQuestTemplate(startQuest.QuestID);

		if (quest == null)
			return;

		var adventureMapPOI = CliDB.AdventureMapPOIStorage.Values.FirstOrDefault(adventureMap => { return adventureMap.QuestID == startQuest.QuestID && _player.MeetPlayerCondition(adventureMap.PlayerConditionID); });

		if (adventureMapPOI == null)
			return;

		if (_player.CanTakeQuest(quest, true))
			_player.AddQuestAndCheckCompletion(quest, _player);
	}
}