// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Forged.RealmServer.Entities;
using Forged.RealmServer.Networking;
using Forged.RealmServer.Networking.Packets;
using Forged.RealmServer.Networking.Packets.Quest;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Forged.RealmServer.Scripting.Interfaces.IQuest;

namespace Forged.RealmServer;

public partial class WorldSession
{
	[WorldPacketHandler(ClientOpcodes.QuestConfirmAccept)]
	void HandleQuestConfirmAccept(QuestConfirmAccept packet)
	{
		var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

		if (quest != null)
		{
			if (!quest.HasFlag(QuestFlags.PartyAccept))
				return;

			var originalPlayer = Global.ObjAccessor.FindPlayer(_player.GetPlayerSharingQuest());

			if (originalPlayer == null)
				return;

			if (!_player.IsInSameRaidWith(originalPlayer))
				return;

			if (!originalPlayer.IsActiveQuest(packet.QuestID))
				return;

			if (!_player.CanTakeQuest(quest, true))
				return;

			if (_player.CanAddQuest(quest, true))
			{
				_player.AddQuestAndCheckCompletion(quest, null); // NULL, this prevent DB script from duplicate running

				if (quest.SourceSpellID > 0)
					_player.CastSpell(_player, quest.SourceSpellID, true);
			}
		}

		_player.ClearQuestSharingInfo();
	}

	[WorldPacketHandler(ClientOpcodes.PushQuestToParty)]
	void HandlePushQuestToParty(PushQuestToParty packet)
	{
		var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

		if (quest == null)
			return;

		var sender = Player;

		if (!_player.CanShareQuest(packet.QuestID))
		{
			sender.SendPushToPartyResponse(sender, QuestPushReason.NotAllowed);

			return;
		}

		// in pool and not currently available (wintergrasp weekly, dalaran weekly) - can't share
		if (Global.QuestPoolMgr.IsQuestActive(packet.QuestID))
		{
			sender.SendPushToPartyResponse(sender, QuestPushReason.NotDaily);

			return;
		}

		var group = sender.Group;

		if (!group)
		{
			sender.SendPushToPartyResponse(sender, QuestPushReason.NotInParty);

			return;
		}

		for (var refe = group.FirstMember; refe != null; refe = refe.Next())
		{
			var receiver = refe.Source;

			if (!receiver || receiver == sender)
				continue;

			if (!receiver.GetPlayerSharingQuest().IsEmpty)
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Busy);

				continue;
			}

			if (!receiver.IsAlive)
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Dead);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.DeadToRecipient, quest);

				continue;
			}

			switch (receiver.GetQuestStatus(packet.QuestID))
			{
				case QuestStatus.Rewarded:
				{
					sender.SendPushToPartyResponse(receiver, QuestPushReason.AlreadyDone);
					receiver.SendPushToPartyResponse(sender, QuestPushReason.AlreadyDoneToRecipient, quest);

					continue;
				}
				case QuestStatus.Incomplete:
				case QuestStatus.Complete:
				{
					sender.SendPushToPartyResponse(receiver, QuestPushReason.OnQuest);
					receiver.SendPushToPartyResponse(sender, QuestPushReason.OnQuestToRecipient, quest);

					continue;
				}
				default:
					break;
			}

			if (!receiver.SatisfyQuestLog(false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.LogFull);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.LogFullToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestDay(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.AlreadyDone);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.AlreadyDoneToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestMinLevel(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.LowLevel);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.LowLevelToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestMaxLevel(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.HighLevel);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.HighLevelToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestClass(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Class);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.ClassToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestRace(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Race);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.RaceToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestReputation(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.LowFaction);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.LowFactionToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestDependentQuests(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Prerequisite);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.PrerequisiteToRecipient, quest);

				continue;
			}

			if (!receiver.SatisfyQuestExpansion(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Expansion);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.ExpansionToRecipient, quest);

				continue;
			}

			if (!receiver.CanTakeQuest(quest, false))
			{
				sender.SendPushToPartyResponse(receiver, QuestPushReason.Invalid);
				receiver.SendPushToPartyResponse(sender, QuestPushReason.InvalidToRecipient, quest);

				continue;
			}

			sender.SendPushToPartyResponse(receiver, QuestPushReason.Success);

			if ((quest.IsAutoComplete && quest.IsRepeatable && !quest.IsDailyOrWeekly) || quest.HasFlag(QuestFlags.AutoComplete))
			{
				receiver.PlayerTalkClass.SendQuestGiverRequestItems(quest, sender.GUID, receiver.CanCompleteRepeatableQuest(quest), true);
			}
			else
			{
				receiver.SetQuestSharingInfo(sender.GUID, quest.Id);
				receiver.PlayerTalkClass.SendQuestGiverQuestDetails(quest, receiver.GUID, true, false);

				if (quest.IsAutoAccept && receiver.CanAddQuest(quest, true) && receiver.CanTakeQuest(quest, true))
				{
					receiver.AddQuestAndCheckCompletion(quest, sender);
					sender.SendPushToPartyResponse(receiver, QuestPushReason.Accepted);
					receiver.ClearQuestSharingInfo();
				}
			}
		}
	}

	[WorldPacketHandler(ClientOpcodes.QuestGiverStatusTrackedQuery)]
	void HandleQuestgiverStatusTrackedQueryOpcode(QuestGiverStatusTrackedQuery questGiverStatusTrackedQuery)
	{
		_player.SendQuestGiverStatusMultiple(questGiverStatusTrackedQuery.QuestGiverGUIDs);
	}
}