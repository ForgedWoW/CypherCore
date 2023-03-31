// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Globals;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Framework.Constants;
using Game.Common.Handlers;
using System.Collections.Generic;

namespace Forged.RealmServer;

public class AdventureJournalHandler : IWorldSessionHandler
{
    private readonly WorldSession _session;
    private readonly CliDB _cliDb;
	private readonly DB2Manager _dB2Manager;
    private readonly GameObjectManager _objectManager;

    public AdventureJournalHandler(WorldSession session, CliDB cliDb, DB2Manager dB2Manager, GameObjectManager objectManager)
    {
        _session = session;
        _cliDb = cliDb;
        _dB2Manager = dB2Manager;
        _objectManager = objectManager;
    }

    [WorldPacketHandler(ClientOpcodes.AdventureJournalOpenQuest)]
	void HandleAdventureJournalOpenQuest(AdventureJournalOpenQuest openQuest)
	{
		var uiDisplay = _dB2Manager.GetUiDisplayForClass(_session.Player.Class);

		if (uiDisplay != null)
			if (!_session.Player.MeetPlayerCondition(uiDisplay.AdvGuidePlayerConditionID))
				return;

		var adventureJournal = _cliDb.AdventureJournalStorage.LookupByKey(openQuest.AdventureJournalID);

		if (adventureJournal == null)
			return;

		if (!_session.Player.MeetPlayerCondition(adventureJournal.PlayerConditionID))
			return;

		var quest = _objectManager.GetQuestTemplate(adventureJournal.QuestID);

		if (quest == null)
			return;

		if (_session.Player.CanTakeQuest(quest, true))
			_session.Player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, _session.Player.GUID, true, false);
	}
}