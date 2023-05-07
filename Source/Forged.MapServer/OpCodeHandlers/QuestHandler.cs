// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Networking;
using Forged.MapServer.Networking.Packets.Quest;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IQuest;
using Framework.Constants;
using Game.Common.Handlers;
using Serilog;

namespace Forged.MapServer.OpCodeHandlers;

public class QuestHandler : IWorldSessionHandler
{
    [WorldPacketHandler(ClientOpcodes.ChoiceResponse)]
    private void HandlePlayerChoiceResponse(ChoiceResponse choiceResponse)
    {
        if (_session.Player.PlayerTalkClass.GetInteractionData().PlayerChoiceId != choiceResponse.ChoiceID)
        {
            Log.Logger.Error($"Error in CMSG_CHOICE_RESPONSE: {GetPlayerInfo()} tried to respond to invalid player choice {choiceResponse.ChoiceID} (allowed {_session.Player.PlayerTalkClass.GetInteractionData().PlayerChoiceId}) (possible packet-hacking detected)");

            return;
        }

        var playerChoice = Global.ObjectMgr.GetPlayerChoice(choiceResponse.ChoiceID);

        if (playerChoice == null)
            return;

        var playerChoiceResponse = playerChoice.GetResponseByIdentifier(choiceResponse.ResponseIdentifier);

        if (playerChoiceResponse == null)
        {
            Log.Logger.Error($"Error in CMSG_CHOICE_RESPONSE: {GetPlayerInfo()} tried to select invalid player choice response {choiceResponse.ResponseIdentifier} (possible packet-hacking detected)");

            return;
        }

        ScriptManager.ForEach<IPlayerOnPlayerChoiceResponse>(p => p.OnPlayerChoiceResponse(_session.Player, (uint)choiceResponse.ChoiceID, (uint)choiceResponse.ResponseIdentifier));

        if (playerChoiceResponse.Reward != null)
        {
            var reward = playerChoiceResponse.Reward;

            if (reward.TitleId != 0)
                _session.Player.SetTitle(CliDB.CharTitlesStorage.LookupByKey(reward.TitleId), false);

            if (reward.PackageId != 0)
                _session.Player.RewardQuestPackage((uint)reward.PackageId);

            if (reward.SkillLineId != 0 && _session.Player.HasSkill((SkillType)reward.SkillLineId))
                _session.Player.UpdateSkillPro((uint)reward.SkillLineId, 1000, reward.SkillPointCount);

            if (reward.HonorPointCount != 0)
                _session.Player.AddHonorXp(reward.HonorPointCount);

            if (reward.Money != 0)
                _session.Player.ModifyMoney((long)reward.Money, false);

            if (reward.Xp != 0)
                _session.Player.GiveXP(reward.Xp, null, 0.0f);

            foreach (var item in reward.Items)
            {
                List<ItemPosCount> dest = new();

                if (_session.Player.CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, item.Id, (uint)item.Quantity) == InventoryResult.Ok)
                {
                    var newItem = _session.Player.StoreNewItem(dest, item.Id, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(item.Id), null, ItemContext.QuestReward, item.BonusListIDs);
                    _session.Player.SendNewItem(newItem, (uint)item.Quantity, true, false);
                }
            }

            foreach (var currency in reward.Currency)
                _session.Player.ModifyCurrency(currency.Id, currency.Quantity);

            foreach (var faction in reward.Faction)
                _session.Player.ReputationMgr.ModifyReputation(CliDB.FactionStorage.LookupByKey(faction.Id), faction.Quantity);
        }
    }

    [WorldPacketHandler(ClientOpcodes.PushQuestToParty)]
    private void HandlePushQuestToParty(PushQuestToParty packet)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest == null)
            return;

        var sender = Player;

        if (!_session.Player.CanShareQuest(packet.QuestID))
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
                receiver.PlayerTalkClass.SendQuestGiverRequestItems(quest, sender.GUID, receiver.CanCompleteRepeatableQuest(quest), true);
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

    [WorldPacketHandler(ClientOpcodes.QueryQuestItemUsability)]
    private void HandleQueryQuestItemUsability(QueryQuestItemUsability request)
    {
        //foreach (var itemGuid in request.ItemGUIDs)
        //{
        //    var item = Player.GetItemByGuid(itemGuid);
        //    Player.HasQuestForItem(item.Template.Id);
        //}
    }

    [WorldPacketHandler(ClientOpcodes.QuestConfirmAccept)]
    private void HandleQuestConfirmAccept(QuestConfirmAccept packet)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest != null)
        {
            if (!quest.HasFlag(QuestFlags.PartyAccept))
                return;

            var originalPlayer = Global.ObjAccessor.FindPlayer(_session.Player.GetPlayerSharingQuest());

            if (originalPlayer == null)
                return;

            if (!_session.Player.IsInSameRaidWith(originalPlayer))
                return;

            if (!originalPlayer.IsActiveQuest(packet.QuestID))
                return;

            if (!_session.Player.CanTakeQuest(quest, true))
                return;

            if (_session.Player.CanAddQuest(quest, true))
            {
                _session.Player.AddQuestAndCheckCompletion(quest, null); // NULL, this prevent DB script from duplicate running

                if (quest.SourceSpellID > 0)
                    _session.Player.CastSpell(_session.Player, quest.SourceSpellID, true);
            }
        }

        _session.Player.ClearQuestSharingInfo();
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverAcceptQuest, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverAcceptQuest(QuestGiverAcceptQuest packet)
    {
        WorldObject obj;

        if (!packet.QuestGiverGUID.IsPlayer)
            obj = Global.ObjAccessor.GetObjectByTypeMask(_session.Player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject | TypeMask.Item);
        else
            obj = Global.ObjAccessor.FindPlayer(packet.QuestGiverGUID);

        var CLOSE_GOSSIP_CLEAR_SHARING_INFO = new Action(() =>
        {
            _session.Player.PlayerTalkClass.SendCloseGossip();
            _session.Player.ClearQuestSharingInfo();
        });

        // no or incorrect quest giver
        if (obj == null)
        {
            CLOSE_GOSSIP_CLEAR_SHARING_INFO();

            return;
        }

        var playerQuestObject = obj.AsPlayer;

        if (playerQuestObject)
        {
            if ((_session.Player.GetPlayerSharingQuest().IsEmpty && _session.Player.GetPlayerSharingQuest() != packet.QuestGiverGUID) || !playerQuestObject.CanShareQuest(packet.QuestID))
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();

                return;
            }

            if (!_session.Player.IsInSameRaidWith(playerQuestObject))
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();

                return;
            }
        }
        else
        {
            if (!obj.HasQuest(packet.QuestID))
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();

                return;
            }
        }

        // some kind of WPE protection
        if (!_session.Player.CanInteractWithQuestGiver(obj))
        {
            CLOSE_GOSSIP_CLEAR_SHARING_INFO();

            return;
        }

        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest != null)
        {
            // prevent cheating
            if (!_session.Player.CanTakeQuest(quest, true))
            {
                CLOSE_GOSSIP_CLEAR_SHARING_INFO();

                return;
            }

            if (!_session.Player.GetPlayerSharingQuest().IsEmpty)
            {
                var player = Global.ObjAccessor.FindPlayer(_session.Player.GetPlayerSharingQuest());

                if (player != null)
                {
                    player.SendPushToPartyResponse(_session.Player, QuestPushReason.Accepted);
                    _session.Player.ClearQuestSharingInfo();
                }
            }

            if (_session.Player.CanAddQuest(quest, true))
            {
                _session.Player.AddQuestAndCheckCompletion(quest, obj);

                if (quest.HasFlag(QuestFlags.PartyAccept))
                {
                    var group = _session.Player.Group;

                    if (group)
                        for (var refe = group.FirstMember; refe != null; refe = refe.Next())
                        {
                            var player = refe.Source;

                            if (!player || player == _session.Player || !player.IsInMap(_session.Player)) // not self and in same map
                                continue;

                            if (player.CanTakeQuest(quest, true))
                            {
                                player.SetQuestSharingInfo(_session.Player.GUID, quest.Id);

                                //need confirmation that any gossip window will close
                                player.PlayerTalkClass.SendCloseGossip();

                                _session.Player.SendQuestConfirmAccept(quest, player);
                            }
                        }
                }

                _session.Player.PlayerTalkClass.SendCloseGossip();

                return;
            }
        }

        CLOSE_GOSSIP_CLEAR_SHARING_INFO();
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverChooseReward, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverChooseReward(QuestGiverChooseReward packet)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest == null)
            return;

        if (packet.Choice.Item.ItemID != 0)
            switch (packet.Choice.LootItemType)
            {
                case LootItemType.Item:
                    var rewardProto = Global.ObjectMgr.GetItemTemplate(packet.Choice.Item.ItemID);

                    if (rewardProto == null)
                    {
                        Log.Logger.Error("Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {0} ({1}) tried to get invalid reward item (Item Entry: {2}) for quest {3} (possible packet-hacking detected)", _session.Player.GetName(), _session.Player.GUID.ToString(), packet.Choice.Item.ItemID, packet.QuestID);

                        return;
                    }

                    var itemValid = false;

                    for (uint i = 0; i < quest.RewChoiceItemsCount; ++i)
                        if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Item && quest.RewardChoiceItemId[i] == packet.Choice.Item.ItemID)
                        {
                            itemValid = true;

                            break;
                        }

                    if (!itemValid && quest.PackageID != 0)
                    {
                        var questPackageItems = Global.DB2Mgr.GetQuestPackageItems(quest.PackageID);

                        if (questPackageItems != null)
                            foreach (var questPackageItem in questPackageItems)
                            {
                                if (questPackageItem.ItemID != packet.Choice.Item.ItemID)
                                    continue;

                                if (_session.Player.CanSelectQuestPackageItem(questPackageItem))
                                {
                                    itemValid = true;

                                    break;
                                }
                            }

                        if (!itemValid)
                        {
                            var questPackageItems1 = Global.DB2Mgr.GetQuestPackageItemsFallback(quest.PackageID);

                            if (questPackageItems1 != null)
                                foreach (var questPackageItem in questPackageItems1)
                                {
                                    if (questPackageItem.ItemID != packet.Choice.Item.ItemID)
                                        continue;

                                    itemValid = true;

                                    break;
                                }
                        }
                    }

                    if (!itemValid)
                    {
                        Log.Logger.Error("Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {0} ({1}) tried to get reward item (Item Entry: {2}) wich is not a reward for quest {3} (possible packet-hacking detected)", _session.Player.GetName(), _session.Player.GUID.ToString(), packet.Choice.Item.ItemID, packet.QuestID);

                        return;
                    }

                    break;
                case LootItemType.Currency:
                    if (!CliDB.CurrencyTypesStorage.HasRecord(packet.Choice.Item.ItemID))
                    {
                        Log.Logger.Error($"Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {_session.Player.GetName()} ({_session.Player.GUID}) tried to get invalid reward currency (Currency ID: {packet.Choice.Item.ItemID}) for quest {packet.QuestID} (possible packet-hacking detected)");

                        return;
                    }

                    var currencyValid = false;

                    for (uint i = 0; i < quest.RewChoiceItemsCount; ++i)
                        if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Currency && quest.RewardChoiceItemId[i] == packet.Choice.Item.ItemID)
                        {
                            currencyValid = true;

                            break;
                        }

                    if (!currencyValid)
                    {
                        Log.Logger.Error($"Error in CMSG_QUESTGIVER_CHOOSE_REWARD: player {_session.Player.GetName()} ({_session.Player.GUID}) tried to get reward currency (Currency ID: {packet.Choice.Item.ItemID}) wich is not a reward for quest {packet.QuestID} (possible packet-hacking detected)");

                        return;
                    }

                    break;
            }

        WorldObject obj = Player;

        if (!quest.HasFlag(QuestFlags.AutoComplete))
        {
            obj = Global.ObjAccessor.GetObjectByTypeMask(_session.Player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

            if (!obj || !obj.HasInvolvedQuest(packet.QuestID))
                return;

            // some kind of WPE protection
            if (!_session.Player.CanInteractWithQuestGiver(obj))
                return;
        }

        if ((!_session.Player.CanSeeStartQuest(quest) && _session.Player.GetQuestStatus(packet.QuestID) == QuestStatus.None) ||
            (_session.Player.GetQuestStatus(packet.QuestID) != QuestStatus.Complete && !quest.IsAutoComplete))
        {
            Log.Logger.Error<string, string, uint>("Error in QuestStatus.Complete: player {0} ({1}) tried to complete quest {2}, but is not allowed to do so (possible packet-hacking or high latency)",
                                                   _session.Player.GetName(),
                                                   _session.Player.GUID.ToString(),
                                                   packet.QuestID);

            return;
        }

        if (_session.Player.CanRewardQuest(quest, true)) // First, check if player is allowed to turn the quest in (all objectives completed). If not, we send players to the offer reward screen
        {
            if (_session.Player.CanRewardQuest(quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID, true)) // Then check if player can receive the reward item (if inventory is not full, if player doesn't have too many unique items, and so on). If not, the client will close the gossip window
            {
                var bg = _session.Player.Battleground;

                if (bg != null)
                    bg.HandleQuestComplete(packet.QuestID, _session.Player);

                Player.RewardQuest(quest, packet.Choice.LootItemType, packet.Choice.Item.ItemID, obj);
            }
        }
        else
            _session.Player.PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverCloseQuest, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverCloseQuest(QuestGiverCloseQuest questGiverCloseQuest)
    {
        if (_session.Player.FindQuestSlot(questGiverCloseQuest.QuestID) >= SharedConst.MaxQuestLogSize)
            return;

        var quest = Global.ObjectMgr.GetQuestTemplate(questGiverCloseQuest.QuestID);

        if (quest == null)
            return;

        ScriptManager.RunScript<IQuestOnAckAutoAccept>(script => script.OnAcknowledgeAutoAccept(_session.Player, quest), quest.ScriptId);
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverCompleteQuest, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverCompleteQuest(QuestGiverCompleteQuest packet)
    {
        var autoCompleteMode = packet.FromScript; // 0 - standart complete quest mode with npc, 1 - auto-complete mode

        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest == null)
            return;

        if (autoCompleteMode && !quest.HasFlag(QuestFlags.AutoComplete))
            return;

        WorldObject obj;

        if (autoCompleteMode)
            obj = Player;
        else
            obj = Global.ObjAccessor.GetObjectByTypeMask(_session.Player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

        if (!obj)
            return;

        if (!autoCompleteMode)
        {
            if (!obj.HasInvolvedQuest(packet.QuestID))
                return;

            // some kind of WPE protection
            if (!_session.Player.CanInteractWithQuestGiver(obj))
                return;
        }
        else
        {
            // Do not allow completing quests on other players.
            if (packet.QuestGiverGUID != _session.Player.GUID)
                return;
        }

        if (!_session.Player.CanSeeStartQuest(quest) && _session.Player.GetQuestStatus(packet.QuestID) == QuestStatus.None)
        {
            Log.Logger.Error<string, string, uint>("Possible hacking attempt: Player {0} ({1}) tried to complete quest [entry: {2}] without being in possession of the quest!",
                                                   _session.Player.GetName(),
                                                   _session.Player.GUID.ToString(),
                                                   packet.QuestID);

            return;
        }

        if (_session.Player.GetQuestStatus(packet.QuestID) != QuestStatus.Complete)
        {
            if (quest.IsRepeatable)
                _session.Player.PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, _session.Player.CanCompleteRepeatableQuest(quest), false);
            else
                _session.Player.PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, _session.Player.CanRewardQuest(quest, false), false);
        }
        else
        {
            if (quest.HasQuestObjectiveType(QuestObjectiveType.Item)) // some items required
                _session.Player.PlayerTalkClass.SendQuestGiverRequestItems(quest, packet.QuestGiverGUID, _session.Player.CanRewardQuest(quest, false), false);
            else // no items required
                _session.Player.PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
        }
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverHello, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverHello(QuestGiverHello packet)
    {
        var creature = _session.Player.GetNPCIfCanInteractWith(packet.QuestGiverGUID, NPCFlags.QuestGiver, NPCFlags2.None);

        if (creature == null)
        {
            Log.Logger.Debug("WORLD: HandleQuestgiverHello - {0} not found or you can't interact with him.", packet.QuestGiverGUID.ToString());

            return;
        }

        // remove fake death
        if (_session.Player.HasUnitState(UnitState.Died))
            _session.Player.RemoveAurasByType(AuraType.FeignDeath);

        // Stop the npc if moving
        var pause = creature.MovementTemplate.GetInteractionPauseTimer();

        if (pause != 0)
            creature.PauseMovement(pause);

        creature.HomePosition = creature.Location;

        _session.Player.PlayerTalkClass.ClearMenus();

        if (creature.AI.OnGossipHello(_session.Player))
            return;

        _session.Player.PrepareGossipMenu(creature, creature.Template.GossipMenuId, true);
        _session.Player.SendPreparedGossip(creature);
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverQueryQuest, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverQueryQuest(QuestGiverQueryQuest packet)
    {
        // Verify that the guid is valid and is a questgiver or involved in the requested quest
        var obj = Global.ObjAccessor.GetObjectByTypeMask(_session.Player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject | TypeMask.Item);

        if (!obj || (!obj.HasQuest(packet.QuestID) && !obj.HasInvolvedQuest(packet.QuestID)))
        {
            _session.Player.PlayerTalkClass.SendCloseGossip();

            return;
        }

        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest != null)
        {
            if (!_session.Player.CanTakeQuest(quest, true))
                return;

            if (quest.IsAutoAccept && _session.Player.CanAddQuest(quest, true))
                _session.Player.AddQuestAndCheckCompletion(quest, obj);

            if (quest.IsAutoComplete)
                _session.Player.PlayerTalkClass.SendQuestGiverRequestItems(quest, obj.GUID, _session.Player.CanCompleteQuest(quest.Id), true);
            else
                _session.Player.PlayerTalkClass.SendQuestGiverQuestDetails(quest, obj.GUID, true, false);
        }
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverRequestReward, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverRequestReward(QuestGiverRequestReward packet)
    {
        var obj = Global.ObjAccessor.GetObjectByTypeMask(_session.Player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

        if (obj == null || !obj.HasInvolvedQuest(packet.QuestID))
            return;

        // some kind of WPE protection
        if (!_session.Player.CanInteractWithQuestGiver(obj))
            return;

        if (_session.Player.CanCompleteQuest(packet.QuestID))
            _session.Player.CompleteQuest(packet.QuestID);

        if (_session.Player.GetQuestStatus(packet.QuestID) != QuestStatus.Complete)
            return;

        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest != null)
            _session.Player.PlayerTalkClass.SendQuestGiverOfferReward(quest, packet.QuestGiverGUID, true);
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverStatusMultipleQuery)]
    private void HandleQuestgiverStatusMultipleQuery(QuestGiverStatusMultipleQuery packet)
    {
        _session.Player.SendQuestGiverStatusMultiple();
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverStatusQuery, Processing = PacketProcessing.Inplace)]
    private void HandleQuestgiverStatusQuery(QuestGiverStatusQuery packet)
    {
        var questStatus = QuestGiverStatus.None;

        var questgiver = Global.ObjAccessor.GetObjectByTypeMask(_session.Player, packet.QuestGiverGUID, TypeMask.Unit | TypeMask.GameObject);

        if (!questgiver)
        {
            Log.Logger.Information("Error in CMSG_QUESTGIVER_STATUS_QUERY, called for non-existing questgiver {0}", packet.QuestGiverGUID.ToString());

            return;
        }

        switch (questgiver.TypeId)
        {
            case TypeId.Unit:
                if (!questgiver.AsCreature.IsHostileTo(Player)) // do not show quest status to enemies
                    questStatus = _session.Player.GetQuestDialogStatus(questgiver);

                break;
            case TypeId.GameObject:
                questStatus = _session.Player.GetQuestDialogStatus(questgiver);

                break;
            default:
                Log.Logger.Error("QuestGiver called for unexpected type {0}", questgiver.TypeId);

                break;
        }

        //inform client about status of quest
        _session.Player.PlayerTalkClass.SendQuestGiverStatus(questStatus, packet.QuestGiverGUID);
    }

    [WorldPacketHandler(ClientOpcodes.QuestGiverStatusTrackedQuery)]
    private void HandleQuestgiverStatusTrackedQueryOpcode(QuestGiverStatusTrackedQuery questGiverStatusTrackedQuery)
    {
        _session.Player.SendQuestGiverStatusMultiple(questGiverStatusTrackedQuery.QuestGiverGUIDs);
    }

    [WorldPacketHandler(ClientOpcodes.QuestLogRemoveQuest, Processing = PacketProcessing.Inplace)]
    private void HandleQuestLogRemoveQuest(QuestLogRemoveQuest packet)
    {
        if (packet.Entry < SharedConst.MaxQuestLogSize)
        {
            var questId = _session.Player.GetQuestSlotQuestId(packet.Entry);

            if (questId != 0)
            {
                if (!_session.Player.TakeQuestSourceItem(questId, true))
                    return; // can't un-equip some items, reject quest cancel

                var quest = Global.ObjectMgr.GetQuestTemplate(questId);
                var oldStatus = _session.Player.GetQuestStatus(questId);

                if (quest != null)
                {
                    if (quest.LimitTime != 0)
                        _session.Player.RemoveTimedQuest(questId);

                    if (quest.HasFlag(QuestFlags.Pvp))
                    {
                        _session.Player.PvpInfo.IsHostile = _session.Player.PvpInfo.IsInHostileArea || _session.Player.HasPvPForcingQuest();
                        _session.Player.UpdatePvPState();
                    }
                }

                _session.Player.SetQuestSlot(packet.Entry, 0);
                _session.Player.TakeQuestSourceItem(questId, true); // remove quest src item from player
                _session.Player.AbandonQuest(questId);              // remove all quest items player received before abandoning quest. Note, this does not remove normal drop items that happen to be quest requirements. 
                _session.Player.RemoveActiveQuest(questId);
                _session.Player.RemoveCriteriaTimer(CriteriaStartEvent.AcceptQuest, questId);

                Log.Logger.Information<string, uint>("Player {0} abandoned quest {1}", _session.Player.GUID.ToString(), questId);

                ScriptManager.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(_session.Player, questId));

                if (quest != null)
                    ScriptManager.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(_session.Player, quest, oldStatus, QuestStatus.None), quest.ScriptId);
            }

            _session.Player.UpdateCriteria(CriteriaType.AbandonAnyQuest, 1);
        }
    }

    [WorldPacketHandler(ClientOpcodes.QuestPushResult)]
    private void HandleQuestPushResult(QuestPushResult packet)
    {
        if (!_session.Player.GetPlayerSharingQuest().IsEmpty)
        {
            if (_session.Player.GetPlayerSharingQuest() == packet.SenderGUID)
            {
                var player = Global.ObjAccessor.FindPlayer(_session.Player.GetPlayerSharingQuest());

                if (player)
                    player.SendPushToPartyResponse(_session.Player, packet.Result);
            }

            _session.Player.ClearQuestSharingInfo();
        }
    }

    [WorldPacketHandler(ClientOpcodes.QueryQuestInfo, Processing = PacketProcessing.Inplace)]
    private void HandleQuestQuery(QueryQuestInfo packet)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(packet.QuestID);

        if (quest != null)
            _session.Player.PlayerTalkClass.SendQuestQueryResponse(quest);
        else
        {
            QueryQuestInfoResponse response = new()
            {
                QuestID = packet.QuestID
            };

            SendPacket(response);
        }
    }

    [WorldPacketHandler(ClientOpcodes.RequestWorldQuestUpdate)]
    private void HandleRequestWorldQuestUpdate(RequestWorldQuestUpdate packet)
    {
        WorldQuestUpdateResponse response = new();

        // @todo: 7.x Has to be implemented
        //response.WorldQuestUpdates.push_back(WorldPackets::QuestId::WorldQuestUpdateInfo(lastUpdate, questID, timer, variableID, value));

        SendPacket(response);
    }

    [WorldPacketHandler(ClientOpcodes.UiMapQuestLinesRequest, Processing = PacketProcessing.Inplace)]
    private void HandleUiMapQuestLinesRequest(UiMapQuestLinesRequest request)
    {
        var response = new UiMapQuestLinesResponse
        {
            UiMapID = request.UiMapID
        };

        if (DB2Manager.Instance.QuestPOIBlobEntriesByMapId.TryGetValue(request.UiMapID, out var questPOIBlobEntries))
            foreach (var questPOIBlob in questPOIBlobEntries)
                if (Player.MeetPlayerCondition(questPOIBlob.PlayerConditionID) && DB2Manager.Instance.QuestLinesByQuest.TryGetValue((uint)questPOIBlob.QuestID, out var lineXQuestRecords))
                    foreach (var lineXRecord in lineXQuestRecords)
                        if (DB2Manager.Instance.TryGetQuestsForQuestLine(lineXRecord.QuestID, out var questLineQuests))
                            foreach (var questLineQuest in questLineQuests)
                                if (Global.ObjectMgr.TryGetQuestTemplate(questLineQuest.QuestID, out var quest) &&
                                    Player.CanTakeQuest(quest, false) &&
                                    CliDB.ContentTuningStorage.TryGetValue(quest.ContentTuningId, out var contentTune) &&
                                    Player.Level >= contentTune.MinLevel)
                                {
                                    response.QuestLineXQuestIDs.Add(questLineQuest.QuestID);

                                    break;
                                }


        SendPacket(response);
    }
}