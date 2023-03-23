// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.DataStorage;
using Framework.Constants;
using Game.Networking;
using Game.Networking.Packets;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
	void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest openQuest)
	{
		var uiDisplay = Global.DB2Mgr.GetUiDisplayForClass(_player.Class);

		if (uiDisplay != null)
			if (!_player.MeetPlayerCondition(uiDisplay.AdvGuidePlayerConditionID))
				return;

		var adventureJournal = CliDB.AdventureJournalStorage.LookupByKey(openQuest.AdventureJournalID);

		if (adventureJournal == null)
			return;

		if (!_player.MeetPlayerCondition(adventureJournal.PlayerConditionID))
			return;

		var quest = Global.ObjectMgr.GetQuestTemplate(adventureJournal.QuestID);

		if (quest == null)
			return;

		if (_player.CanTakeQuest(quest, true))
			_player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, _player.GUID, true, false);
	}
}