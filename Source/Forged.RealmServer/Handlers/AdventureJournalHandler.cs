// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.RealmServer.DataStorage;
using Framework.Constants;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking.Packets;
using Game.Common.Handlers;
using Forged.RealmServer.DataStorage.ClientReader;

namespace Forged.RealmServer;

public class AdventureJournalHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly Player _player;
    private readonly DB6Storage<AdventureJournalRecord> _adventureJournalStorage;
	private readonly DB2Manager _dB2Manager;

    public AdventureJournalHandler(WorldSession session, Player player, DB6Storage<AdventureJournalRecord> adventureJournalStorage, DB2Manager dB2Manager)
    {
        _session = session;
        _player = player;
        _adventureJournalStorage = adventureJournalStorage;
        _dB2Manager = dB2Manager;
    }

    [WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
	void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest openQuest)
	{
		var uiDisplay = _dB2Manager.GetUiDisplayForClass(_player.Class);

		if (uiDisplay != null)
			if (!_player.MeetPlayerCondition(uiDisplay.AdvGuidePlayerConditionID))
				return;

		var adventureJournal = _adventureJournalStorage.LookupByKey(openQuest.AdventureJournalID);

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