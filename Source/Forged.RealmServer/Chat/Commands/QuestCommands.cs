// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Framework.Constants;
using Forged.RealmServer.DataStorage;
using Game.Entities;
using Forged.RealmServer.Scripting.Interfaces.IPlayer;
using Forged.RealmServer.Scripting.Interfaces.IQuest;
using Game.Common.Entities.Items;
using Game.Common.Entities.Objects;
using Game.Common.Entities.Players;

namespace Forged.RealmServer.Chat;

[CommandGroup("quest")]
class QuestCommands
{
	[Command("add", RBACPermissions.CommandQuestAdd)]
	static bool HandleQuestAdd(CommandHandler handler, Quest quest)
	{
		var player = handler.SelectedPlayer;

		if (!player)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		if (Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, null))
		{
			handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);

			return false;
		}

		// check item starting quest (it can work incorrectly if added without item in inventory)
		var itc = Global.ObjectMgr.GetItemTemplates();
		var result = itc.Values.FirstOrDefault(p => p.StartQuest == quest.Id);

		if (result != null)
		{
			handler.SendSysMessage(CypherStrings.CommandQuestStartfromitem, quest.Id, result.Id);

			return false;
		}

		if (player.IsActiveQuest(quest.Id))
			return false;

		// ok, normal (creature/GO starting) quest
		if (player.CanAddQuest(quest, true))
			player.AddQuestAndCheckCompletion(quest, null);

		return true;
	}

	[Command("complete", RBACPermissions.CommandQuestComplete)]
	static bool HandleQuestComplete(CommandHandler handler, Quest quest)
	{
		var player = handler.SelectedPlayer;

		if (!player)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		// If player doesn't have the quest
		if (player.GetQuestStatus(quest.Id) == QuestStatus.None || Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, null))
		{
			handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);

			return false;
		}

		foreach (var obj in quest.Objectives)
			CompleteObjective(player, obj);

		player.CompleteQuest(quest.Id);

		return true;
	}

	[Command("remove", RBACPermissions.CommandQuestRemove)]
	static bool HandleQuestRemove(CommandHandler handler, Quest quest)
	{
		var player = handler.SelectedPlayer;

		if (!player)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		var oldStatus = player.GetQuestStatus(quest.Id);

		if (oldStatus != QuestStatus.None)
		{
			// remove all quest entries for 'entry' from quest log
			for (byte slot = 0; slot < SharedConst.MaxQuestLogSize; ++slot)
			{
				var logQuest = player.GetQuestSlotQuestId(slot);

				if (logQuest == quest.Id)
				{
					player.SetQuestSlot(slot, 0);

					// we ignore unequippable quest items in this case, its' still be equipped
					player.TakeQuestSourceItem(logQuest, false);

					if (quest.HasFlag(QuestFlags.Pvp))
					{
						player.PvpInfo.IsHostile = player.PvpInfo.IsInHostileArea || player.HasPvPForcingQuest();
						player.UpdatePvPState();
					}
				}
			}

			player.RemoveActiveQuest(quest.Id, false);
			player.RemoveRewardedQuest(quest.Id);

			Global.ScriptMgr.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(player, quest.Id));
			Global.ScriptMgr.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(player, quest, oldStatus, QuestStatus.None), quest.ScriptId);

			handler.SendSysMessage(CypherStrings.CommandQuestRemoved);

			return true;
		}
		else
		{
			handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);

			return false;
		}
	}

	[Command("reward", RBACPermissions.CommandQuestReward)]
	static bool HandleQuestReward(CommandHandler handler, Quest quest)
	{
		var player = handler.SelectedPlayer;

		if (!player)
		{
			handler.SendSysMessage(CypherStrings.NoCharSelected);

			return false;
		}

		// If player doesn't have the quest
		if (player.GetQuestStatus(quest.Id) != QuestStatus.Complete || Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, null))
		{
			handler.SendSysMessage(CypherStrings.CommandQuestNotfound, quest.Id);

			return false;
		}

		player.RewardQuest(quest, LootItemType.Item, 0, player);

		return true;
	}

	static void CompleteObjective(Player player, QuestObjective obj)
	{
		switch (obj.Type)
		{
			case QuestObjectiveType.Item:
			{
				var curItemCount = player.GetItemCount((uint)obj.ObjectID, true);
				List<ItemPosCount> dest = new();
				var msg = player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, (uint)obj.ObjectID, (uint)(obj.Amount - curItemCount));

				if (msg == InventoryResult.Ok)
				{
					var item = player.StoreNewItem(dest, (uint)obj.ObjectID, true);
					player.SendNewItem(item, (uint)(obj.Amount - curItemCount), true, false);
				}

				break;
			}
			case QuestObjectiveType.Monster:
			{
				var creatureInfo = Global.ObjectMgr.GetCreatureTemplate((uint)obj.ObjectID);

				if (creatureInfo != null)
					for (var z = 0; z < obj.Amount; ++z)
						player.KilledMonster(creatureInfo, ObjectGuid.Empty);

				break;
			}
			case QuestObjectiveType.GameObject:
			{
				for (var z = 0; z < obj.Amount; ++z)
					player.KillCreditGO((uint)obj.ObjectID);

				break;
			}
			case QuestObjectiveType.MinReputation:
			{
				var curRep = player.ReputationMgr.GetReputation((uint)obj.ObjectID);

				if (curRep < obj.Amount)
				{
					var factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);

					if (factionEntry != null)
						player.ReputationMgr.SetReputation(factionEntry, obj.Amount);
				}

				break;
			}
			case QuestObjectiveType.MaxReputation:
			{
				var curRep = player.ReputationMgr.GetReputation((uint)obj.ObjectID);

				if (curRep > obj.Amount)
				{
					var factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);

					if (factionEntry != null)
						player.ReputationMgr.SetReputation(factionEntry, obj.Amount);
				}

				break;
			}
			case QuestObjectiveType.Money:
			{
				player.ModifyMoney(obj.Amount);

				break;
			}
			case QuestObjectiveType.PlayerKills:
			{
				for (var z = 0; z < obj.Amount; ++z)
					player.KilledPlayerCredit(ObjectGuid.Empty);

				break;
			}
		}
	}

	[CommandGroup("objective")]
	class ObjectiveCommands
	{
		[Command("complete", RBACPermissions.CommandQuestObjectiveComplete)]
		static bool HandleQuestObjectiveComplete(CommandHandler handler, uint objectiveId)
		{
			var player = handler.SelectedPlayerOrSelf;

			if (!player)
			{
				handler.SendSysMessage(CypherStrings.NoCharSelected);

				return false;
			}

			var obj = Global.ObjectMgr.GetQuestObjective(objectiveId);

			if (obj == null)
			{
				handler.SendSysMessage(CypherStrings.QuestObjectiveNotfound);

				return false;
			}

			CompleteObjective(player, obj);

			return true;
		}
	}
}