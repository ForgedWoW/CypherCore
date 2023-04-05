// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Chrono;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DataStorage.Structs.Q;
using Forged.MapServer.Entities.Creatures;
using Forged.MapServer.Entities.Items;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Objects.Update;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Mails;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Misc;
using Forged.MapServer.Networking.Packets.Quest;
using Forged.MapServer.Phasing;
using Forged.MapServer.Quest;
using Forged.MapServer.Scripting.Interfaces.IItem;
using Forged.MapServer.Scripting.Interfaces.IPlayer;
using Forged.MapServer.Scripting.Interfaces.IQuest;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Serilog;

namespace Forged.MapServer.Entities.Players;

public partial class Player
{
    public void AbandonQuest(uint questId)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(questId);

        if (quest != null)
        {
            // Destroy quest items on quest abandon.
            foreach (var obj in quest.Objectives)
                if (obj.Type == QuestObjectiveType.Item)
                {
                    var itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)obj.ObjectID);

                    if (itemTemplate != null)
                        if (itemTemplate.Bonding == ItemBondingType.Quest)
                            DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true);
                }

            // Destroy items received during the quest.
            for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                var itemTemplate = Global.ObjectMgr.GetItemTemplate(quest.ItemDrop[i]);

                if (itemTemplate != null)
                    if (quest.ItemDropQuantity[i] != 0 && itemTemplate.Bonding == ItemBondingType.Quest)
                        DestroyItemCount(quest.ItemDrop[i], quest.ItemDropQuantity[i], true);
            }
        }
    }

    public void AddQuest(Quest.Quest quest, WorldObject questGiver)
    {
        var logSlot = FindQuestSlot(0);

        if (logSlot >= SharedConst.MaxQuestLogSize) // Player does not have any free slot in the quest log
            return;

        var questId = quest.Id;

        // if not exist then created with set uState == NEW and rewarded=false
        if (!_mQuestStatus.ContainsKey(questId))
            _mQuestStatus[questId] = new QuestStatusData();

        var questStatusData = _mQuestStatus.LookupByKey(questId);
        var oldStatus = questStatusData.Status;

        // check for repeatable quests status reset
        SetQuestSlot(logSlot, questId);
        questStatusData.Slot = logSlot;
        questStatusData.Status = QuestStatus.Incomplete;
        questStatusData.Explored = false;

        foreach (var obj in quest.Objectives)
        {
            _questObjectiveStatus.Add((obj.Type, obj.ObjectID),
                                      new QuestObjectiveStatusData()
                                      {
                                          QuestStatusPair = (questId, questStatusData),
                                          Objective = obj
                                      });

            switch (obj.Type)
            {
                case QuestObjectiveType.MinReputation:
                case QuestObjectiveType.MaxReputation:
                    var factionEntry = CliDB.FactionStorage.LookupByKey(obj.ObjectID);

                    if (factionEntry != null)
                        ReputationMgr.SetVisible(factionEntry);

                    break;
                case QuestObjectiveType.CriteriaTree:
                    _questObjectiveCriteriaManager.ResetCriteriaTree((uint)obj.ObjectID);

                    break;
                default:
                    break;
            }
        }

        GiveQuestSourceItem(quest);
        AdjustQuestObjectiveProgress(quest);

        long endTime = 0;
        var limittime = quest.LimitTime;

        if (limittime != 0)
        {
            // shared timed quest
            if (questGiver != null && questGiver.IsTypeId(TypeId.Player))
                limittime = questGiver.AsPlayer._mQuestStatus[questId].Timer / Time.IN_MILLISECONDS;

            AddTimedQuest(questId);
            questStatusData.Timer = limittime * Time.IN_MILLISECONDS;
            endTime = GameTime.CurrentTime + limittime;
        }
        else
        {
            questStatusData.Timer = 0;
        }

        if (quest.HasFlag(QuestFlags.Pvp))
        {
            PvpInfo.IsHostile = true;
            UpdatePvPState();
        }

        if (quest.SourceSpellID > 0)
        {
            var spellInfo = SpellManager.GetSpellInfo(quest.SourceSpellID, Location.Map.DifficultyID);
            Unit caster = this;

            if (questGiver != null && questGiver.IsTypeMask(TypeMask.Unit) && !quest.HasFlag(QuestFlags.PlayerCastOnAccept) && !spellInfo.HasTargetType(Targets.UnitCaster) && !spellInfo.HasTargetType(Targets.DestCasterSummon))
            {
                var unit = questGiver.AsUnit;

                if (unit != null)
                    caster = unit;
            }

            caster.CastSpell(this, spellInfo.Id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetCastDifficulty(spellInfo.Difficulty));
        }

        SetQuestSlotEndTime(logSlot, endTime);
        SetQuestSlotAcceptTime(logSlot, GameTime.CurrentTime);

        _questStatusSave[questId] = QuestSaveType.Default;

        StartCriteriaTimer(CriteriaStartEvent.AcceptQuest, questId);

        SendQuestUpdate(questId);

        ScriptManager.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(this, questId));
        ScriptManager.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(this, quest, oldStatus, questStatusData.Status), quest.ScriptId);
    }

    public void AddQuestAndCheckCompletion(Quest.Quest quest, WorldObject questGiver)
    {
        AddQuest(quest, questGiver);

        foreach (var obj in quest.Objectives)
            if (obj.Type == QuestObjectiveType.CriteriaTree)
                if (_questObjectiveCriteriaManager.HasCompletedObjective(obj))
                    KillCreditCriteriaTreeObjective(obj);

        if (CanCompleteQuest(quest.Id))
            CompleteQuest(quest.Id);

        if (!questGiver)
            return;

        switch (questGiver.TypeId)
        {
            case TypeId.Unit:
                PlayerTalkClass.ClearMenus();
                questGiver.AsCreature.AI.OnQuestAccept(this, quest);

                break;
            case TypeId.Item:
            case TypeId.Container:
            case TypeId.AzeriteItem:
            case TypeId.AzeriteEmpoweredItem:
            {
                var item = (Item)questGiver;
                ScriptManager.RunScriptRet<IItemOnQuestAccept>(p => p.OnQuestAccept(this, item, quest), item.ScriptId);

                // There are two cases where the source item is not destroyed when the quest is accepted:
                // - It is required to finish the quest, and is an unique item
                // - It is the same item present in the source item field (item that would be given on quest accept)
                var destroyItem = true;

                foreach (var obj in quest.Objectives)
                    if (obj.Type == QuestObjectiveType.Item && obj.ObjectID == item.Entry && item.Template.MaxCount > 0)
                    {
                        destroyItem = false;

                        break;
                    }

                if (quest.SourceItemId == item.Entry)
                    destroyItem = false;

                if (destroyItem)
                    DestroyItem(item.BagSlot, item.Slot, true);

                break;
            }
            case TypeId.GameObject:
                PlayerTalkClass.ClearMenus();
                questGiver.AsGameObject.AI.OnQuestAccept(this, quest);

                break;
            default:
                break;
        }
    }

    public void AdjustQuestObjectiveProgress(Quest.Quest quest)
    {
        // adjust progress of quest objectives that rely on external counters, like items
        if (quest.HasQuestObjectiveType(QuestObjectiveType.Item))
            foreach (var obj in quest.Objectives)
                if (obj.Type == QuestObjectiveType.Item)
                {
                    var reqItemCount = (uint)obj.Amount;
                    var curItemCount = GetItemCount((uint)obj.ObjectID, true);
                    SetQuestObjectiveData(obj, (int)Math.Min(curItemCount, reqItemCount));
                }
                else if (obj.Type == QuestObjectiveType.HaveCurrency)
                {
                    var reqCurrencyCount = (uint)obj.Amount;
                    var curCurrencyCount = GetCurrencyQuantity((uint)obj.ObjectID);
                    SetQuestObjectiveData(obj, (int)Math.Min(reqCurrencyCount, curCurrencyCount));
                }
    }

    public void AreaExploredOrEventHappens(uint questId)
    {
        if (questId != 0)
        {
            var status = _mQuestStatus.LookupByKey(questId);

            if (status != null)
                // Dont complete failed quest
                if (!status.Explored && status.Status != QuestStatus.Failed)
                {
                    status.Explored = true;
                    _questStatusSave[questId] = QuestSaveType.Default;

                    SendQuestComplete(questId);
                }

            if (CanCompleteQuest(questId))
                CompleteQuest(questId);
        }
    }

    public bool CanAddQuest(Quest.Quest quest, bool msg)
    {
        if (!SatisfyQuestLog(msg))
            return false;

        var srcitem = quest.SourceItemId;

        if (srcitem > 0)
        {
            var count = quest.SourceItemIdCount;
            List<ItemPosCount> dest = new();
            var msg2 = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, srcitem, count);

            // player already have max number (in most case 1) source item, no additional item needed and quest can be added.
            if (msg2 == InventoryResult.ItemMaxCount)
                return true;

            if (msg2 != InventoryResult.Ok)
            {
                SendEquipError(msg2, null, null, srcitem);

                return false;
            }
        }

        return true;
    }

    public bool CanCompleteQuest(uint questId, uint ignoredQuestObjectiveId = 0)
    {
        if (questId != 0)
        {
            var qInfo = Global.ObjectMgr.GetQuestTemplate(questId);

            if (qInfo == null)
                return false;

            if (!qInfo.IsRepeatable && GetQuestRewardStatus(questId))
                return false; // not allow re-complete quest

            // auto complete quest
            if (qInfo.IsAutoComplete && CanTakeQuest(qInfo, false))
                return true;

            var qStatus = _mQuestStatus.LookupByKey(questId);

            if (qStatus == null)
                return false;

            if (qStatus.Status == QuestStatus.Incomplete)
            {
                foreach (var obj in qInfo.Objectives)
                {
                    if (ignoredQuestObjectiveId != 0 && obj.Id == ignoredQuestObjectiveId)
                        continue;

                    if (!obj.Flags.HasAnyFlag(QuestObjectiveFlags.Optional) && !obj.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar))
                        if (!IsQuestObjectiveComplete(qStatus.Slot, qInfo, obj))
                            return false;
                }

                if (qInfo.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent) && !qStatus.Explored)
                    return false;

                if (qInfo.LimitTime != 0 && qStatus.Timer == 0)
                    return false;

                return true;
            }
        }

        return false;
    }

    public bool CanCompleteRepeatableQuest(Quest.Quest quest)
    {
        // Solve problem that player don't have the quest and try complete it.
        // if repeatable she must be able to complete event if player don't have it.
        // Seem that all repeatable quest are DELIVER Flag so, no need to add more.
        if (!CanTakeQuest(quest, false))
            return false;

        if (quest.HasQuestObjectiveType(QuestObjectiveType.Item))
            foreach (var obj in quest.Objectives)
                if (obj.Type == QuestObjectiveType.Item && !HasItemCount((uint)obj.ObjectID, (uint)obj.Amount))
                    return false;

        if (!CanRewardQuest(quest, false))
            return false;

        return true;
    }

    public bool CanInteractWithQuestGiver(WorldObject questGiver)
    {
        switch (questGiver.TypeId)
        {
            case TypeId.Unit:
                return GetNPCIfCanInteractWith(questGiver.GUID, NPCFlags.QuestGiver, NPCFlags2.None) != null;
            case TypeId.GameObject:
                return GetGameObjectIfCanInteractWith(questGiver.GUID, GameObjectTypes.QuestGiver) != null;
            case TypeId.Player:
                return IsAlive && questGiver.AsPlayer.IsAlive;
            case TypeId.Item:
                return IsAlive;
            default:
                break;
        }

        return false;
    }

    public bool CanRewardQuest(Quest.Quest quest, bool msg)
    {
        // quest is disabled
        if (Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, this))
            return false;

        // not auto complete quest and not completed quest (only cheating case, then ignore without message)
        if (!quest.IsDFQuest && !quest.IsAutoComplete && GetQuestStatus(quest.Id) != QuestStatus.Complete)
            return false;

        // daily quest can't be rewarded (25 daily quest already completed)
        if (!SatisfyQuestDay(quest, msg) || !SatisfyQuestWeek(quest, msg) || !SatisfyQuestMonth(quest, msg) || !SatisfyQuestSeasonal(quest, msg))
            return false;

        // player no longer satisfies the quest's requirements (skill level etc.)
        if (!SatisfyQuestLevel(quest, msg) || !SatisfyQuestSkill(quest, msg) || !SatisfyQuestReputation(quest, msg))
            return false;

        // rewarded and not repeatable quest (only cheating case, then ignore without message)
        if (GetQuestRewardStatus(quest.Id))
            return false;

        // prevent receive reward with quest items in bank
        if (quest.HasQuestObjectiveType(QuestObjectiveType.Item))
            foreach (var obj in quest.Objectives)
            {
                if (obj.Type != QuestObjectiveType.Item)
                    continue;

                if (GetItemCount((uint)obj.ObjectID) < obj.Amount)
                {
                    if (msg)
                        SendEquipError(InventoryResult.ItemNotFound, null, null, (uint)obj.ObjectID);

                    return false;
                }
            }

        foreach (var obj in quest.Objectives)
            switch (obj.Type)
            {
                case QuestObjectiveType.Currency:
                    if (!HasCurrency((uint)obj.ObjectID, (uint)obj.Amount))
                        return false;

                    break;
                case QuestObjectiveType.Money:
                    if (!HasEnoughMoney(obj.Amount))
                        return false;

                    break;
            }

        return true;
    }

    public bool CanRewardQuest(Quest.Quest quest, LootItemType rewardType, uint rewardId, bool msg)
    {
        List<ItemPosCount> dest = new();

        if (quest.RewChoiceItemsCount > 0)
            switch (rewardType)
            {
                case LootItemType.Item:
                    for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                        if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Item && quest.RewardChoiceItemId[i] == rewardId)
                        {
                            var res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, quest.RewardChoiceItemId[i], quest.RewardChoiceItemCount[i]);

                            if (res != InventoryResult.Ok)
                            {
                                if (msg)
                                    SendQuestFailed(quest.Id, res);

                                return false;
                            }
                        }

                    break;
                case LootItemType.Currency:
                    break;
            }

        if (quest.RewItemsCount > 0)
            for (uint i = 0; i < quest.RewItemsCount; ++i)
                if (quest.RewardItemId[i] != 0)
                {
                    var res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, quest.RewardItemId[i], quest.RewardItemCount[i]);

                    if (res != InventoryResult.Ok)
                    {
                        if (msg)
                            SendQuestFailed(quest.Id, res);

                        return false;
                    }
                }

        // QuestPackageItem.db2
        if (quest.PackageID != 0)
        {
            var hasFilteredQuestPackageReward = false;
            var questPackageItems = DB2Manager.GetQuestPackageItems(quest.PackageID);

            if (questPackageItems != null)
                foreach (var questPackageItem in questPackageItems)
                {
                    if (questPackageItem.ItemID != rewardId)
                        continue;

                    if (CanSelectQuestPackageItem(questPackageItem))
                    {
                        hasFilteredQuestPackageReward = true;
                        var res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity);

                        if (res != InventoryResult.Ok)
                        {
                            SendEquipError(res, null, null, questPackageItem.ItemID);

                            return false;
                        }
                    }
                }

            if (!hasFilteredQuestPackageReward)
            {
                var questPackageItems1 = DB2Manager.GetQuestPackageItemsFallback(quest.PackageID);

                if (questPackageItems1 != null)
                    foreach (var questPackageItem in questPackageItems1)
                    {
                        if (questPackageItem.ItemID != rewardId)
                            continue;

                        var res = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity);

                        if (res != InventoryResult.Ok)
                        {
                            SendEquipError(res, null, null, questPackageItem.ItemID);

                            return false;
                        }
                    }
            }
        }

        return true;
    }

    public bool CanSeeStartQuest(Quest.Quest quest)
    {
        if (!Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, this) &&
            SatisfyQuestClass(quest, false) &&
            SatisfyQuestRace(quest, false) &&
            SatisfyQuestSkill(quest, false) &&
            SatisfyQuestExclusiveGroup(quest, false) &&
            SatisfyQuestReputation(quest, false) &&
            SatisfyQuestDependentQuests(quest, false) &&
            SatisfyQuestDay(quest, false) &&
            SatisfyQuestWeek(quest, false) &&
            SatisfyQuestMonth(quest, false) &&
            SatisfyQuestSeasonal(quest, false))
            return Level + GetDefaultValue("Quests.HighLevelHideDiff", 7) >= GetQuestMinLevel(quest);

        return false;
    }

    public bool CanSelectQuestPackageItem(QuestPackageItemRecord questPackageItem)
    {
        var rewardProto = Global.ObjectMgr.GetItemTemplate(questPackageItem.ItemID);

        if (rewardProto == null)
            return false;

        if ((rewardProto.HasFlag(ItemFlags2.FactionAlliance) && Team != TeamFaction.Alliance) ||
            (rewardProto.HasFlag(ItemFlags2.FactionHorde) && Team != TeamFaction.Horde))
            return false;

        switch (questPackageItem.DisplayType)
        {
            case QuestPackageFilter.LootSpecialization:
                return rewardProto.IsUsableByLootSpecialization(this, true);
            case QuestPackageFilter.Class:
                return rewardProto.ItemSpecClassMask == 0 || (rewardProto.ItemSpecClassMask & ClassMask) != 0;
            case QuestPackageFilter.Everyone:
                return true;
            default:
                break;
        }

        return false;
    }

    public bool CanShareQuest(uint questID)
    {
        var qInfo = Global.ObjectMgr.GetQuestTemplate(questID);

        if (qInfo != null && qInfo.HasFlag(QuestFlags.Sharable))
        {
            var questStatusData = _mQuestStatus.LookupByKey(questID);

            return questStatusData != null;
        }

        return false;
    }

    public bool CanTakeQuest(Quest.Quest quest, bool msg)
    {
        return !Global.DisableMgr.IsDisabledFor(DisableType.Quest, quest.Id, this) && SatisfyQuestStatus(quest, msg) && SatisfyQuestExclusiveGroup(quest, msg) && SatisfyQuestClass(quest, msg) && SatisfyQuestRace(quest, msg) && SatisfyQuestLevel(quest, msg) && SatisfyQuestSkill(quest, msg) && SatisfyQuestReputation(quest, msg) && SatisfyQuestDependentQuests(quest, msg) && SatisfyQuestTimed(quest, msg) && SatisfyQuestDay(quest, msg) && SatisfyQuestWeek(quest, msg) && SatisfyQuestMonth(quest, msg) && SatisfyQuestSeasonal(quest, msg) && SatisfyQuestConditions(quest, msg);
    }

    public void ClearQuestSharingInfo()
    {
        _playerSharingQuest = ObjectGuid.Empty;
        _sharedQuestId = 0;
    }

    public void CompleteQuest(uint questID)
    {
        if (questID != 0)
        {
            SetQuestStatus(questID, QuestStatus.Complete);

            var questStatus = _mQuestStatus.LookupByKey(questID);

            if (questStatus != null)
                SetQuestSlotState(questStatus.Slot, QuestSlotStateMask.Complete);

            var qInfo = Global.ObjectMgr.GetQuestTemplate(questID);

            if (qInfo != null)
                if (qInfo.HasFlag(QuestFlags.Tracking))
                    RewardQuest(qInfo, LootItemType.Item, 0, this, false);
        }
    }

    public void DailyReset()
    {
        foreach (var questId in ActivePlayerData.DailyQuestsCompleted)
        {
            var questBit = DB2Manager.GetQuestUniqueBitFlag(questId);

            if (questBit != 0)
                SetQuestCompletedBit(questBit, false);
        }

        DailyQuestsReset dailyQuestsReset = new()
        {
            Count = ActivePlayerData.DailyQuestsCompleted.Size()
        };

        SendPacket(dailyQuestsReset);

        ClearDynamicUpdateFieldValues(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DailyQuestsCompleted));

        _dfQuests.Clear(); // Dungeon Finder Quests.

        // DB data deleted in caller
        _dailyQuestChanged = false;
        _lastDailyQuestTime = 0;

        Garrison?.ResetFollowerActivationLimit();
    }

    public void FailQuest(uint questId)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(questId);

        if (quest != null)
        {
            var questStatus = GetQuestStatus(questId);

            // we can only fail incomplete quest or...
            if (questStatus != QuestStatus.Incomplete)
                // completed timed quest with no requirements
                if (questStatus != QuestStatus.Complete || quest.LimitTime == 0 || !quest.Objectives.Empty())
                    return;

            SetQuestStatus(questId, QuestStatus.Failed);

            var logSlot = FindQuestSlot(questId);

            if (logSlot < SharedConst.MaxQuestLogSize)
                SetQuestSlotState(logSlot, QuestSlotStateMask.Fail);

            if (quest.LimitTime != 0)
            {
                var qStatus = _mQuestStatus[questId];

                RemoveTimedQuest(questId);
                qStatus.Timer = 0;

                SendQuestTimerFailed(questId);
            }
            else
            {
                SendQuestFailed(questId);
            }

            // Destroy quest items on quest failure.
            foreach (var obj in quest.Objectives)
                if (obj.Type == QuestObjectiveType.Item)
                {
                    var itemTemplate = Global.ObjectMgr.GetItemTemplate((uint)obj.ObjectID);

                    if (itemTemplate != null)
                        if (itemTemplate.Bonding == ItemBondingType.Quest)
                            DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true);
                }

            // Destroy items received during the quest.
            for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
            {
                var itemTemplate = Global.ObjectMgr.GetItemTemplate(quest.ItemDrop[i]);

                if (itemTemplate != null)
                    if (quest.ItemDropQuantity[i] != 0 && itemTemplate.Bonding == ItemBondingType.Quest)
                        DestroyItemCount(quest.ItemDrop[i], quest.ItemDropQuantity[i], true);
            }
        }
    }

    public ushort FindQuestSlot(uint questID)
    {
        for (ushort i = 0; i < SharedConst.MaxQuestLogSize; ++i)
            if (GetQuestSlotQuestId(i) == questID)
                return i;

        return SharedConst.MaxQuestLogSize;
    }

    public Quest.Quest GetNextQuest(ObjectGuid guid, Quest.Quest quest)
    {
        QuestRelationResult quests;
        var nextQuestID = quest.NextQuestInChain;

        switch (guid.High)
        {
            case HighGuid.Player:
                return Global.ObjectMgr.GetQuestTemplate(nextQuestID);
            case HighGuid.Creature:
            case HighGuid.Pet:
            case HighGuid.Vehicle:
            {
                var creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);

                if (creature != null)
                    quests = Global.ObjectMgr.GetCreatureQuestRelations(creature.Entry);
                else
                    return null;

                break;
            }
            case HighGuid.GameObject:
            {
                //we should obtain map from GetMap() in 99% of cases. Special case
                //only for quests which cast teleport spells on player
                var map = Location.IsInWorld ? Location.Map : Global.MapMgr.FindMap(Location.MapId, InstanceId);

                var gameObject = map.GetGameObject(guid);

                if (gameObject != null)
                    quests = Global.ObjectMgr.GetGOQuestRelations(gameObject.Entry);
                else
                    return null;

                break;
            }
            default:
                return null;
        }

        if (nextQuestID != 0)
            if (quests.HasQuest(nextQuestID))
                return Global.ObjectMgr.GetQuestTemplate(nextQuestID);

        return null;
    }

    public ObjectGuid GetPlayerSharingQuest()
    {
        return _playerSharingQuest;
    }

    public QuestGiverStatus GetQuestDialogStatus(WorldObject questgiver)
    {
        QuestRelationResult questRelations;
        QuestRelationResult questInvolvedRelations;

        switch (questgiver.TypeId)
        {
            case TypeId.GameObject:
            {
                var ai = questgiver.AsGameObject.AI;

                var questStatus = ai?.GetDialogStatus(this);

                if (questStatus.HasValue)
                    return questStatus.Value;

                questRelations = Global.ObjectMgr.GetGOQuestRelations(questgiver.Entry);
                questInvolvedRelations = Global.ObjectMgr.GetGOQuestInvolvedRelations(questgiver.Entry);

                break;
            }
            case TypeId.Unit:
            {
                var ai = questgiver.AsCreature.AI;

                var questStatus = ai?.GetDialogStatus(this);

                if (questStatus.HasValue)
                    return questStatus.Value;

                questRelations = Global.ObjectMgr.GetCreatureQuestRelations(questgiver.Entry);
                questInvolvedRelations = Global.ObjectMgr.GetCreatureQuestInvolvedRelations(questgiver.Entry);

                break;
            }
            default:
                // it's impossible, but check
                Log.Logger.Error("GetQuestDialogStatus called for unexpected type {0}", questgiver.TypeId);

                return QuestGiverStatus.None;
        }

        var result = QuestGiverStatus.None;

        foreach (var questId in questInvolvedRelations)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (quest == null)
                continue;

            switch (GetQuestStatus(questId))
            {
                case QuestStatus.Complete:
                    if (quest.QuestTag == QuestTagType.CovenantCalling)
                        result |= quest.HasFlag(QuestFlags.HideRewardPoi) ? QuestGiverStatus.CovenantCallingRewardCompleteNoPOI : QuestGiverStatus.CovenantCallingRewardCompletePOI;
                    else if (quest.HasFlagEx(QuestFlagsEx.LegendaryQuest))
                        result |= quest.HasFlag(QuestFlags.HideRewardPoi) ? QuestGiverStatus.LegendaryRewardCompleteNoPOI : QuestGiverStatus.LegendaryRewardCompletePOI;
                    else
                        result |= quest.HasFlag(QuestFlags.HideRewardPoi) ? QuestGiverStatus.RewardCompleteNoPOI : QuestGiverStatus.RewardCompletePOI;

                    break;
                case QuestStatus.Incomplete:
                    if (quest.QuestTag == QuestTagType.CovenantCalling)
                        result |= QuestGiverStatus.CovenantCallingReward;
                    else
                        result |= QuestGiverStatus.Reward;

                    break;
                default:
                    break;
            }

            if (quest.IsAutoComplete && CanTakeQuest(quest, false) && quest.IsRepeatable && !quest.IsDailyOrWeekly && !quest.IsMonthly)
            {
                if (Level <= (GetQuestLevel(quest) + GetDefaultValue("Quests.LowLevelHideDiff", 4)))
                    result |= QuestGiverStatus.RepeatableTurnin;
                else
                    result |= QuestGiverStatus.TrivialRepeatableTurnin;
            }
        }

        foreach (var questId in questRelations)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (quest == null)
                continue;

            if (!ConditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.QuestAvailable, quest.Id, this))
                continue;

            if (GetQuestStatus(questId) == QuestStatus.None)
                if (CanSeeStartQuest(quest))
                {
                    if (SatisfyQuestLevel(quest, false))
                    {
                        if (Level <= (GetQuestLevel(quest) + GetDefaultValue("Quests.LowLevelHideDiff", 4)))
                        {
                            if (quest.QuestTag == QuestTagType.CovenantCalling)
                                result |= QuestGiverStatus.CovenantCallingQuest;
                            else if (quest.HasFlagEx(QuestFlagsEx.LegendaryQuest))
                                result |= QuestGiverStatus.LegendaryQuest;
                            else if (quest.IsDaily)
                                result |= QuestGiverStatus.DailyQuest;
                            else
                                result |= QuestGiverStatus.Quest;
                        }
                        else if (quest.IsDaily)
                        {
                            result |= QuestGiverStatus.TrivialDailyQuest;
                        }
                        else
                        {
                            result |= QuestGiverStatus.Trivial;
                        }
                    }
                    else
                    {
                        result |= QuestGiverStatus.Future;
                    }
                }
        }

        return result;
    }

    public int GetQuestLevel(Quest.Quest quest)
    {
        if (quest == null)
            return 0;

        return GetQuestLevel(quest.ContentTuningId);
    }

    public int GetQuestLevel(uint contentTuningId)
    {
        var questLevels = DB2Manager.GetContentTuningData(contentTuningId, PlayerData.CtrOptions.Value.ContentTuningConditionMask);

        if (questLevels.HasValue)
        {
            var minLevel = GetQuestMinLevel(contentTuningId);
            int maxLevel = questLevels.Value.MaxLevel;
            var level = (int)Level;

            if (level >= minLevel)
                return Math.Min(level, maxLevel);

            return minLevel;
        }

        return 0;
    }

    public int GetQuestMinLevel(Quest.Quest quest)
    {
        return GetQuestMinLevel(quest.ContentTuningId);
    }

    public uint GetQuestMoneyReward(Quest.Quest quest)
    {
        return quest.MoneyValue(this) * GetDefaultValue("Rate.QuestId.Money.Reward", 1.0f);
    }

    public int GetQuestObjectiveData(QuestObjective objective)
    {
        var slot = FindQuestSlot(objective.QuestID);

        if (slot >= SharedConst.MaxQuestLogSize)
            return 0;

        return GetQuestSlotObjectiveData(slot, objective);
    }

    public bool GetQuestRewardStatus(uint questID)
    {
        var qInfo = Global.ObjectMgr.GetQuestTemplate(questID);

        if (qInfo != null)
        {
            if (qInfo.IsSeasonal && !qInfo.IsRepeatable)
                return !SatisfyQuestSeasonal(qInfo, false);

            // for repeatable quests: rewarded field is set after first reward only to prevent getting XP more than once
            if (!qInfo.IsRepeatable)
                return IsQuestRewarded(questID);

            return false;
        }

        return false;
    }

    public uint GetQuestSlotAcceptTime(ushort slot)
    {
        return PlayerData.QuestLog[slot].AcceptTime;
    }

    public ushort GetQuestSlotCounter(ushort slot, byte counter)
    {
        if (counter < SharedConst.MaxQuestCounts)
            return PlayerData.QuestLog[slot].ObjectiveProgress[counter];

        return 0;
    }

    public uint GetQuestSlotEndTime(ushort slot)
    {
        return PlayerData.QuestLog[slot].EndTime;
    }

    public int GetQuestSlotObjectiveData(ushort slot, QuestObjective objective)
    {
        if (objective.StorageIndex < 0)
        {
            Log.Logger.Error($"Player.GetQuestObjectiveData: Called for quest {objective.QuestID} with invalid StorageIndex {objective.StorageIndex} (objective data is not tracked)");

            return 0;
        }

        if (objective.StorageIndex >= SharedConst.MaxQuestCounts)
        {
            Log.Logger.Error($"Player.GetQuestObjectiveData: Player '{GetName()}' ({GUID}) quest {objective.QuestID} out of range StorageIndex {objective.StorageIndex}");

            return 0;
        }

        if (!objective.IsStoringFlag())
            return GetQuestSlotCounter(slot, (byte)objective.StorageIndex);

        return GetQuestSlotObjectiveFlag(slot, objective.StorageIndex) ? 1 : 0;
    }

    public uint GetQuestSlotQuestId(ushort slot)
    {
        return PlayerData.QuestLog[slot].QuestID;
    }

    public uint GetQuestSlotState(ushort slot, byte counter)
    {
        return PlayerData.QuestLog[slot].StateFlags;
    }

    public QuestStatus GetQuestStatus(uint questId)
    {
        if (questId != 0)
        {
            var questStatusData = _mQuestStatus.LookupByKey(questId);

            if (questStatusData != null)
                return questStatusData.Status;

            if (GetQuestRewardStatus(questId))
                return QuestStatus.Rewarded;
        }

        return QuestStatus.None;
    }

    public uint GetQuestXPReward(Quest.Quest quest)
    {
        var rewarded = IsQuestRewarded(quest.Id) && !quest.IsDFQuest;

        // Not give XP in case already completed once repeatable quest
        if (rewarded)
            return 0;

        var xp = quest.XPValue(this) * GetDefaultValue("Rate.XP.QuestId", 1.0f);

        // handle SPELL_AURA_MOD_XP_QUEST_PCT auras
        var modXPPctAuras = GetAuraEffectsByType(AuraType.ModXpQuestPct);

        foreach (var eff in modXPPctAuras)
            MathFunctions.AddPct(ref xp, eff.Amount);

        return xp;
    }

    public ushort GetReqKillOrCastCurrentCount(uint questID, int entry)
    {
        var qInfo = Global.ObjectMgr.GetQuestTemplate(questID);

        if (qInfo == null)
            return 0;

        var slot = FindQuestSlot(questID);

        if (slot >= SharedConst.MaxQuestLogSize)
            return 0;

        foreach (var obj in qInfo.Objectives)
            if (obj.ObjectID == entry)
                return (ushort)GetQuestSlotObjectiveData(slot, obj);

        return 0;
    }

    public int GetRewardedQuestCount()
    {
        return _rewardedQuests.Count;
    }

    public List<uint> GetRewardedQuests()
    {
        return _rewardedQuests;
    }

    public uint GetSharedQuestID()
    {
        return _sharedQuestId;
    }
    public bool GiveQuestSourceItem(Quest.Quest quest)
    {
        var srcitem = quest.SourceItemId;

        if (srcitem > 0)
        {
            // Don't give source item if it is the same item used to start the quest
            var itemTemplate = Global.ObjectMgr.GetItemTemplate(srcitem);

            if (quest.Id == itemTemplate.StartQuest)
                return true;

            var count = quest.SourceItemIdCount;

            if (count <= 0)
                count = 1;

            List<ItemPosCount> dest = new();
            var msg = CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, srcitem, count);

            if (msg == InventoryResult.Ok)
            {
                var item = StoreNewItem(dest, srcitem, true);
                SendNewItem(item, count, true, false);

                return true;
            }

            // player already have max amount required item, just report success
            if (msg == InventoryResult.ItemMaxCount)
                return true;

            SendEquipError(msg, null, null, srcitem);

            return false;
        }

        return true;
    }

    public void GroupEventHappens(uint questId, WorldObject pEventObject)
    {
        var group = Group;

        if (group)
            for (var refe = group.FirstMember; refe != null; refe = refe.Next())
            {
                var player = refe.Source;

                // for any leave or dead (with not released body) group member at appropriate distance
                if (player && player.IsAtGroupRewardDistance(pEventObject) && !player.GetCorpse())
                    player.AreaExploredOrEventHappens(questId);
            }
        else
            AreaExploredOrEventHappens(questId);
    }

    public bool HasPvPForcingQuest()
    {
        for (byte i = 0; i < SharedConst.MaxQuestLogSize; ++i)
        {
            var questId = GetQuestSlotQuestId(i);

            if (questId == 0)
                continue;

            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (quest == null)
                continue;

            if (quest.HasFlag(QuestFlags.Pvp))
                return true;
        }

        return false;
    }

    public bool HasQuestForGO(int goId)
    {
        foreach (var objectiveStatusData in _questObjectiveStatus.LookupByKey((QuestObjectiveType.GameObject, goId)))
        {
            var qInfo = Global.ObjectMgr.GetQuestTemplate(objectiveStatusData.QuestStatusPair.QuestID);
            var objective = objectiveStatusData.Objective;

            if (!IsQuestObjectiveCompletable(objectiveStatusData.QuestStatusPair.Status.Slot, qInfo, objective))
                continue;

            // hide quest if player is in raid-group and quest is no raid quest
            if (Group && Group.IsRaidGroup && !qInfo.IsAllowedInRaid(Location.Map.DifficultyID))
                if (!InBattleground) //there are two ways.. we can make every bg-quest a raidquest, or add this code here.. i don't know if this can be exploited by other quests, but i think all other quests depend on a specific area.. but keep this in mind, if something strange happens later
                    continue;

            if (!IsQuestObjectiveComplete(objectiveStatusData.QuestStatusPair.Status.Slot, qInfo, objective))
                return true;
        }

        return false;
    }

    public bool HasQuestForItem(uint itemid)
    {
        // Search incomplete objective first
        foreach (var objectiveItr in _questObjectiveStatus.LookupByKey((QuestObjectiveType.Item, (int)itemid)))
        {
            var qInfo = Global.ObjectMgr.GetQuestTemplate(objectiveItr.QuestStatusPair.QuestID);
            var objective = objectiveItr.Objective;

            if (!IsQuestObjectiveCompletable(objectiveItr.QuestStatusPair.Status.Slot, qInfo, objective))
                continue;

            // hide quest if player is in raid-group and quest is no raid quest
            if (Group && Group.IsRaidGroup && !qInfo.IsAllowedInRaid(Location.Map.DifficultyID))
                if (!InBattleground) //there are two ways.. we can make every bg-quest a raidquest, or add this code here.. i don't know if this can be exploited by other quests, but i think all other quests depend on a specific area.. but keep this in mind, if something strange happens later
                    continue;

            if (!IsQuestObjectiveComplete(objectiveItr.QuestStatusPair.Status.Slot, qInfo, objective))
                return true;
        }

        // This part - for ItemDrop
        foreach (var questStatus in _mQuestStatus)
        {
            if (questStatus.Value.Status != QuestStatus.Incomplete)
                continue;

            var qInfo = Global.ObjectMgr.GetQuestTemplate(questStatus.Key);

            // hide quest if player is in raid-group and quest is no raid quest
            if (Group && Group.IsRaidGroup && !qInfo.IsAllowedInRaid(Location.Map.DifficultyID))
                if (!InBattleground)
                    continue;

            for (byte j = 0; j < SharedConst.QuestItemDropCount; ++j)
            {
                // examined item is a source item
                if (qInfo.ItemDrop[j] != itemid)
                    continue;

                var pProto = Global.ObjectMgr.GetItemTemplate(itemid);

                // allows custom amount drop when not 0
                var maxAllowedCount = qInfo.ItemDropQuantity[j] != 0 ? qInfo.ItemDropQuantity[j] : pProto.MaxStackSize;

                // 'unique' item
                if (pProto.MaxCount != 0 && pProto.MaxCount < maxAllowedCount)
                    maxAllowedCount = pProto.MaxCount;

                if (GetItemCount(itemid, true) < maxAllowedCount)
                    return true;
            }
        }

        return false;
    }

    public void IncompleteQuest(uint questID)
    {
        if (questID != 0)
        {
            SetQuestStatus(questID, QuestStatus.Incomplete);

            var logSlot = FindQuestSlot(questID);

            if (logSlot < SharedConst.MaxQuestLogSize)
                RemoveQuestSlotState(logSlot, QuestSlotStateMask.Complete);
        }
    }

    public bool IsActiveQuest(uint questID)
    {
        return _mQuestStatus.ContainsKey(questID);
    }

    public bool IsDailyQuestDone(uint questID)
    {
        return ActivePlayerData.DailyQuestsCompleted.FindIndex(questID) >= 0;
    }

    public bool IsQuestObjectiveCompletable(ushort slot, Quest.Quest quest, QuestObjective objective)
    {
        if (objective.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar))
        {
            // delegate check to actual progress bar objective
            var progressBarObjective = quest.Objectives.Find(otherObjective => otherObjective.Type == QuestObjectiveType.ProgressBar && !otherObjective.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar));

            if (progressBarObjective == null)
                return false;

            return IsQuestObjectiveCompletable(slot, quest, progressBarObjective) && !IsQuestObjectiveComplete(slot, quest, progressBarObjective);
        }

        var objectiveIndex = quest.Objectives.IndexOf(objective);

        if (objectiveIndex == 0)
            return true;

        // check sequenced objectives
        var previousIndex = objectiveIndex - 1;
        var objectiveSequenceSatisfied = true;
        var previousSequencedObjectiveComplete = false;
        var previousSequencedObjectiveIndex = -1;

        do
        {
            var previousObjective = quest.Objectives[previousIndex];

            if (previousObjective.Flags.HasAnyFlag(QuestObjectiveFlags.Sequenced))
            {
                previousSequencedObjectiveIndex = previousIndex;
                previousSequencedObjectiveComplete = IsQuestObjectiveComplete(slot, quest, previousObjective);

                break;
            }

            if (objectiveSequenceSatisfied)
                objectiveSequenceSatisfied = IsQuestObjectiveComplete(slot, quest, previousObjective) || previousObjective.Flags.HasAnyFlag(QuestObjectiveFlags.Optional | QuestObjectiveFlags.PartOfProgressBar);

            --previousIndex;
        } while (previousIndex >= 0);

        if (objective.Flags.HasAnyFlag(QuestObjectiveFlags.Sequenced))
        {
            if (previousSequencedObjectiveIndex == -1)
                return objectiveSequenceSatisfied;

            if (!previousSequencedObjectiveComplete || !objectiveSequenceSatisfied)
                return false;
        }
        else if (!previousSequencedObjectiveComplete && previousSequencedObjectiveIndex != -1)
        {
            if (!IsQuestObjectiveCompletable(slot, quest, quest.Objectives[previousSequencedObjectiveIndex]))
                return false;
        }

        return true;
    }

    public bool IsQuestObjectiveComplete(ushort slot, Quest.Quest quest, QuestObjective objective)
    {
        switch (objective.Type)
        {
            case QuestObjectiveType.Monster:
            case QuestObjectiveType.Item:
            case QuestObjectiveType.GameObject:
            case QuestObjectiveType.TalkTo:
            case QuestObjectiveType.PlayerKills:
            case QuestObjectiveType.WinPvpPetBattles:
            case QuestObjectiveType.HaveCurrency:
            case QuestObjectiveType.ObtainCurrency:
            case QuestObjectiveType.IncreaseReputation:
                if (GetQuestSlotObjectiveData(slot, objective) < objective.Amount)
                    return false;

                break;
            case QuestObjectiveType.MinReputation:
                if (ReputationMgr.GetReputation((uint)objective.ObjectID) < objective.Amount)
                    return false;

                break;
            case QuestObjectiveType.MaxReputation:
                if (ReputationMgr.GetReputation((uint)objective.ObjectID) > objective.Amount)
                    return false;

                break;
            case QuestObjectiveType.Money:
                if (!HasEnoughMoney(objective.Amount))
                    return false;

                break;
            case QuestObjectiveType.AreaTrigger:
            case QuestObjectiveType.WinPetBattleAgainstNpc:
            case QuestObjectiveType.DefeatBattlePet:
            case QuestObjectiveType.CriteriaTree:
            case QuestObjectiveType.AreaTriggerEnter:
            case QuestObjectiveType.AreaTriggerExit:
                if (GetQuestSlotObjectiveData(slot, objective) == 0)
                    return false;

                break;
            case QuestObjectiveType.LearnSpell:
                if (!HasSpell((uint)objective.ObjectID))
                    return false;

                break;
            case QuestObjectiveType.Currency:
                if (!HasCurrency((uint)objective.ObjectID, (uint)objective.Amount))
                    return false;

                break;
            case QuestObjectiveType.ProgressBar:
                if (!IsQuestObjectiveProgressBarComplete(slot, quest))
                    return false;

                break;
            default:
                Log.Logger.Error("Player.CanCompleteQuest: Player '{0}' ({1}) tried to complete a quest (ID: {2}) with an unknown objective type {3}",
                                 GetName(),
                                 GUID.ToString(),
                                 objective.QuestID,
                                 objective.Type);

                return false;
        }

        return true;
    }

    public bool IsQuestObjectiveProgressBarComplete(ushort slot, Quest.Quest quest)
    {
        var progress = 0.0f;

        foreach (var obj in quest.Objectives)
            if (obj.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar))
            {
                progress += GetQuestSlotObjectiveData(slot, obj) * obj.ProgressBarWeight;

                if (progress >= 100.0f)
                    return true;
            }

        return false;
    }

    public bool IsQuestRewarded(uint questID)
    {
        return _rewardedQuests.Contains(questID);
    }

    public void ItemAddedQuestCheck(uint entry, uint count)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.Item, (int)entry, count);
    }

    public void ItemRemovedQuestCheck(uint entry, uint count)
    {
        foreach (var objectiveStatusData in _questObjectiveStatus.LookupByKey((QuestObjectiveType.Item, (int)entry)))
        {
            var questId = objectiveStatusData.QuestStatusPair.QuestID;
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);
            var logSlot = objectiveStatusData.QuestStatusPair.Status.Slot;
            var objective = objectiveStatusData.Objective;

            if (!IsQuestObjectiveCompletable(logSlot, quest, objective))
                continue;

            var newItemCount = (int)GetItemCount(entry); // we may have more than what the status shows, so we have to iterate inventory

            if (newItemCount < objective.Amount)
            {
                SetQuestObjectiveData(objective, newItemCount);
                IncompleteQuest(questId);
            }
        }

        UpdateVisibleGameobjectsOrSpellClicks();
    }

    public void KillCreditCriteriaTreeObjective(QuestObjective questObjective)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.CriteriaTree, questObjective.ObjectID, 1);
    }

    public void KillCreditGO(uint entry, ObjectGuid guid = default)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.GameObject, (int)entry, 1, guid);
    }

    public void KilledMonster(CreatureTemplate cInfo, ObjectGuid guid)
    {
        if (cInfo.Entry != 0)
            KilledMonsterCredit(cInfo.Entry, guid);

        for (byte i = 0; i < 2; ++i)
            if (cInfo.KillCredit[i] != 0)
                KilledMonsterCredit(cInfo.KillCredit[i]);
    }

    public void KilledMonsterCredit(uint entry, ObjectGuid guid = default)
    {
        ushort addKillCount = 1;
        var realEntry = entry;
        Creature killed = null;

        if (!guid.IsEmpty)
        {
            killed = Location.Map.GetCreature(guid);

            if (killed != null && killed.Entry != 0)
                realEntry = killed.Entry;
        }

        StartCriteriaTimer(CriteriaStartEvent.KillNPC, realEntry); // MUST BE CALLED FIRST
        UpdateCriteria(CriteriaType.KillCreature, realEntry, addKillCount, 0, killed);

        UpdateQuestObjectiveProgress(QuestObjectiveType.Monster, (int)entry, 1, guid);
    }

    public void KilledPlayerCredit(ObjectGuid victimGuid)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.PlayerKills, 0, 1, victimGuid);
    }

    public void LearnQuestRewardedSpells(Quest.Quest quest)
    {
        //wtf why is rewardspell a uint if it can me -1
        var spellID = Convert.ToInt32(quest.RewardSpell);
        var srcSpellID = quest.SourceSpellID;

        // skip quests without rewarded spell
        if (spellID == 0)
            return;

        // if RewSpellCast = -1 we remove aura do to SrcSpell from player.
        if (spellID == -1 && srcSpellID != 0)
        {
            RemoveAura(srcSpellID);

            return;
        }

        var spellInfo = SpellManager.GetSpellInfo((uint)spellID, Difficulty.None);

        if (spellInfo == null)
            return;

        // check learned spells state
        var found = false;

        foreach (var spellEffectInfo in spellInfo.Effects)
            if (spellEffectInfo.IsEffect(SpellEffectName.LearnSpell) && !HasSpell(spellEffectInfo.TriggerSpell))
            {
                found = true;

                break;
            }

        // skip quests with not teaching spell or already known spell
        if (!found)
            return;

        var effect = spellInfo.GetEffect(0);
        var learned0 = effect.TriggerSpell;

        if (!HasSpell(learned0))
        {
            found = false;
            var skills = SpellManager.GetSkillLineAbilityMapBounds(learned0);

            foreach (var skillLine in skills)
                if (skillLine.AcquireMethod == AbilityLearnType.RewardedFromQuest)
                {
                    found = true;

                    break;
                }

            // profession specialization can be re-learned from npc
            if (!found)
                return;
        }

        CastSpell(this, (uint)spellID, true);
    }

    public void LearnQuestRewardedSpells()
    {
        // learn spells received from quest completing
        foreach (var questId in _rewardedQuests)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (quest == null)
                continue;

            LearnQuestRewardedSpells(quest);
        }
    }

    public void MoneyChanged(ulong value)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.Money, 0, (long)value - (long)Money);
    }

    public void PrepareQuestMenu(ObjectGuid guid)
    {
        QuestRelationResult questRelations;
        QuestRelationResult questInvolvedRelations;

        // pets also can have quests
        var creature = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);

        if (creature != null)
        {
            questRelations = Global.ObjectMgr.GetCreatureQuestRelations(creature.Entry);
            questInvolvedRelations = Global.ObjectMgr.GetCreatureQuestInvolvedRelations(creature.Entry);
        }
        else
        {
            //we should obtain map from GetMap() in 99% of cases. Special case
            //only for quests which cast teleport spells on player
            var map = Location.IsInWorld ? Location.Map : Global.MapMgr.FindMap(Location.MapId, InstanceId);

            var gameObject = map.GetGameObject(guid);

            if (gameObject != null)
            {
                questRelations = Global.ObjectMgr.GetGOQuestRelations(gameObject.Entry);
                questInvolvedRelations = Global.ObjectMgr.GetGOQuestInvolvedRelations(gameObject.Entry);
            }
            else
            {
                return;
            }
        }

        var qm = PlayerTalkClass.GetQuestMenu();
        qm.ClearMenu();

        foreach (var questId in questInvolvedRelations)
        {
            var status = GetQuestStatus(questId);

            if (status == QuestStatus.Complete)
                qm.AddMenuItem(questId, 4);
            else if (status == QuestStatus.Incomplete)
                qm.AddMenuItem(questId, 4);
        }

        foreach (var questId in questRelations)
        {
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (quest == null)
                continue;

            if (!CanTakeQuest(quest, false))
                continue;

            if (quest.IsAutoComplete && (!quest.IsRepeatable || quest.IsDaily || quest.IsWeekly || quest.IsMonthly))
                qm.AddMenuItem(questId, 0);
            else if (quest.IsAutoComplete)
                qm.AddMenuItem(questId, 4);
            else if (GetQuestStatus(questId) == QuestStatus.None)
                qm.AddMenuItem(questId, 2);
        }
    }

    public void RemoveActiveQuest(uint questId, bool update = true)
    {
        var questStatus = _mQuestStatus.LookupByKey(questId);

        if (questStatus != null)
        {
            _questObjectiveStatus.RemoveIfMatching((objective) => objective.Value.QuestStatusPair.Status == questStatus);
            _mQuestStatus.Remove(questId);
            _questStatusSave[questId] = QuestSaveType.Delete;
        }

        if (update)
            SendQuestUpdate(questId);
    }

    public void RemoveQuestSlotState(ushort slot, QuestSlotStateMask state)
    {
        var questLogField = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        RemoveUpdateFieldFlagValue(questLogField.ModifyValue(questLogField.StateFlags), (uint)state);
    }

    public void RemoveRewardedQuest(uint questId, bool update = true)
    {
        if (_rewardedQuests.Contains(questId))
        {
            _rewardedQuests.Remove(questId);
            _rewardedQuestsSave[questId] = QuestSaveType.ForceDelete;
        }

        var questBit = DB2Manager.GetQuestUniqueBitFlag(questId);

        if (questBit != 0)
            SetQuestCompletedBit(questBit, false);

        // Remove seasonal quest also
        var qInfo = Global.ObjectMgr.GetQuestTemplate(questId);

        if (qInfo.IsSeasonal)
        {
            var eventId = qInfo.EventIdForQuest;

            if (_seasonalquests.ContainsKey(eventId))
            {
                _seasonalquests[eventId].Remove(questId);
                _seasonalQuestChanged = true;
            }
        }

        if (update)
            SendQuestUpdate(questId);
    }

    public void RemoveTimedQuest(uint questId)
    {
        _timedquests.Remove(questId);
    }

    public void ReputationChanged(FactionRecord factionRecord, int change)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.MinReputation, (int)factionRecord.Id, change);
        UpdateQuestObjectiveProgress(QuestObjectiveType.MaxReputation, (int)factionRecord.Id, change);
        UpdateQuestObjectiveProgress(QuestObjectiveType.IncreaseReputation, (int)factionRecord.Id, change);
    }

    public void ResetMonthlyQuestStatus()
    {
        if (_monthlyquests.Empty())
            return;

        foreach (var questId in _monthlyquests)
        {
            var questBit = DB2Manager.GetQuestUniqueBitFlag(questId);

            if (questBit != 0)
                SetQuestCompletedBit(questBit, false);
        }

        _monthlyquests.Clear();
        // DB data deleted in caller
        _monthlyQuestChanged = false;
    }

    public void ResetSeasonalQuestStatus(ushort eventID, long eventStartTime)
    {
        // DB data deleted in caller
        _seasonalQuestChanged = false;

        var eventList = _seasonalquests.LookupByKey(eventID);

        if (eventList == null)
            return;

        foreach (var (questId, completedTime) in eventList.ToList())
            if (completedTime < eventStartTime)
            {
                var questBit = DB2Manager.GetQuestUniqueBitFlag(questId);

                if (questBit != 0)
                    SetQuestCompletedBit(questBit, false);

                eventList.Remove(questId);
            }

        if (eventList.Empty())
            _seasonalquests.Remove(eventID);
    }

    public void ResetWeeklyQuestStatus()
    {
        if (_weeklyquests.Empty())
            return;

        foreach (var questId in _weeklyquests)
        {
            var questBit = DB2Manager.GetQuestUniqueBitFlag(questId);

            if (questBit != 0)
                SetQuestCompletedBit(questBit, false);
        }

        _weeklyquests.Clear();
        // DB data deleted in caller
        _weeklyQuestChanged = false;
    }

    public void RewardQuest(Quest.Quest quest, LootItemType rewardType, uint rewardId, WorldObject questGiver, bool announce = true)
    {
        //this THING should be here to protect code from quest, which cast on player far teleport as a reward
        //should work fine, cause far teleport will be executed in Update()
        SetCanDelayTeleport(true);

        var questId = quest.Id;
        var oldStatus = GetQuestStatus(questId);

        foreach (var obj in quest.Objectives)
            switch (obj.Type)
            {
                case QuestObjectiveType.Item:
                    DestroyItemCount((uint)obj.ObjectID, (uint)obj.Amount, true);

                    break;
                case QuestObjectiveType.Currency:
                    RemoveCurrency((uint)obj.ObjectID, obj.Amount, CurrencyDestroyReason.QuestTurnin);

                    break;
            }

        if (!quest.FlagsEx.HasAnyFlag(QuestFlagsEx.KeepAdditionalItems))
            for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
                if (quest.ItemDrop[i] != 0)
                {
                    var count = quest.ItemDropQuantity[i];
                    DestroyItemCount(quest.ItemDrop[i], count != 0 ? count : 9999, true);
                }

        RemoveTimedQuest(questId);

        if (quest.RewItemsCount > 0)
            for (uint i = 0; i < quest.RewItemsCount; ++i)
            {
                var itemId = quest.RewardItemId[i];

                if (itemId != 0)
                {
                    List<ItemPosCount> dest = new();

                    if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, itemId, quest.RewardItemCount[i]) == InventoryResult.Ok)
                    {
                        var item = StoreNewItem(dest, itemId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(itemId));
                        SendNewItem(item, quest.RewardItemCount[i], true, false);
                    }
                    else if (quest.IsDFQuest)
                    {
                        SendItemRetrievalMail(itemId, quest.RewardItemCount[i], ItemContext.QuestReward);
                    }
                }
            }

        var currencyGainSource = CurrencyGainSource.QuestReward;

        if (quest.IsDaily)
            currencyGainSource = CurrencyGainSource.DailyQuestReward;
        else if (quest.IsWeekly)
            currencyGainSource = CurrencyGainSource.WeeklyQuestReward;
        else if (quest.IsWorldQuest)
            currencyGainSource = CurrencyGainSource.WorldQuestReward;

        switch (rewardType)
        {
            case LootItemType.Item:
                var rewardProto = Global.ObjectMgr.GetItemTemplate(rewardId);

                if (rewardProto != null && quest.RewChoiceItemsCount != 0)
                    for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                        if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Item && quest.RewardChoiceItemId[i] == rewardId)
                        {
                            List<ItemPosCount> dest = new();

                            if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, rewardId, quest.RewardChoiceItemCount[i]) == InventoryResult.Ok)
                            {
                                var item = StoreNewItem(dest, rewardId, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(rewardId));
                                SendNewItem(item, quest.RewardChoiceItemCount[i], true, false);
                            }
                        }


                // QuestPackageItem.db2
                if (rewardProto != null && quest.PackageID != 0)
                    RewardQuestPackage(quest.PackageID, rewardId);

                break;
            case LootItemType.Currency:
                if (CliDB.CurrencyTypesStorage.HasRecord(rewardId) && quest.RewChoiceItemsCount != 0)
                    for (uint i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
                        if (quest.RewardChoiceItemId[i] != 0 && quest.RewardChoiceItemType[i] == LootItemType.Currency && quest.RewardChoiceItemId[i] == rewardId)
                            AddCurrency(quest.RewardChoiceItemId[i], quest.RewardChoiceItemCount[i], currencyGainSource);

                break;
        }

        for (byte i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
            if (quest.RewardCurrencyId[i] != 0)
                AddCurrency(quest.RewardCurrencyId[i], quest.RewardCurrencyCount[i], currencyGainSource);

        var skill = quest.RewardSkillId;

        if (skill != 0)
            UpdateSkillPro(skill, 1000, quest.RewardSkillPoints);

        var logSlot = FindQuestSlot(questId);

        if (logSlot < SharedConst.MaxQuestLogSize)
            SetQuestSlot(logSlot, 0);

        var xp = GetQuestXPReward(quest);

        var moneyRew = 0;

        if (!IsMaxLevel)
            GiveXP(xp, null);
        else
            moneyRew = (int)(quest.GetRewMoneyMaxLevel() * GetDefaultValue("Rate.Drop.Money", 1.0f));

        moneyRew += (int)GetQuestMoneyReward(quest);

        if (moneyRew != 0)
        {
            ModifyMoney(moneyRew);

            if (moneyRew > 0)
                UpdateCriteria(CriteriaType.MoneyEarnedFromQuesting, (uint)moneyRew);

            SendDisplayToast(0, DisplayToastType.Money, false, (uint)moneyRew, DisplayToastMethod.QuestComplete, questId);
        }

        // honor reward
        var honor = quest.CalculateHonorGain(Level);

        if (honor != 0)
            RewardHonor(null, 0, (int)honor);

        // title reward
        if (quest.RewardTitleId != 0)
        {
            var titleEntry = CliDB.CharTitlesStorage.LookupByKey(quest.RewardTitleId);

            if (titleEntry != null)
                SetTitle(titleEntry);
        }

        // Send reward mail
        var mailTemplateID = quest.RewardMailTemplateId;

        if (mailTemplateID != 0)
        {
            SQLTransaction trans = new();
            // @todo Poor design of mail system
            var questMailSender = quest.RewardMailSenderEntry;

            if (questMailSender != 0)
                new MailDraft(mailTemplateID).SendMailTo(trans, this, new MailSender(questMailSender), MailCheckMask.HasBody, quest.RewardMailDelay);
            else
                new MailDraft(mailTemplateID).SendMailTo(trans, this, new MailSender(questGiver), MailCheckMask.HasBody, quest.RewardMailDelay);

            CharacterDatabase.CommitTransaction(trans);
        }

        if (quest.IsDaily || quest.IsDFQuest)
        {
            SetDailyQuestStatus(questId);

            if (quest.IsDaily)
            {
                UpdateCriteria(CriteriaType.CompleteDailyQuest, questId);
                UpdateCriteria(CriteriaType.CompleteAnyDailyQuestPerDay, questId);
            }
        }
        else if (quest.IsWeekly)
        {
            SetWeeklyQuestStatus(questId);
        }
        else if (quest.IsMonthly)
        {
            SetMonthlyQuestStatus(questId);
        }
        else if (quest.IsSeasonal)
        {
            SetSeasonalQuestStatus(questId);
        }

        RemoveActiveQuest(questId, false);

        if (quest.CanIncreaseRewardedQuestCounters())
            SetRewardedQuest(questId);

        SendQuestReward(quest, questGiver?.AsCreature, xp, !announce);

        RewardReputation(quest);

        // cast spells after mark quest complete (some spells have quest completed state requirements in spell_area data)
        if (quest.RewardSpell > 0)
        {
            var spellInfo = SpellManager.GetSpellInfo(quest.RewardSpell, Location.Map.DifficultyID);
            Unit caster = this;

            if (questGiver != null && questGiver.IsTypeMask(TypeMask.Unit) && !quest.HasFlag(QuestFlags.PlayerCastOnComplete) && !spellInfo.HasTargetType(Targets.UnitCaster))
            {
                var unit = questGiver.AsUnit;

                if (unit != null)
                    caster = unit;
            }

            caster.CastSpell(this, spellInfo.Id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetCastDifficulty(spellInfo.Difficulty));
        }
        else
        {
            foreach (var displaySpell in quest.RewardDisplaySpell)
            {
                var playerCondition = CliDB.PlayerConditionStorage.LookupByKey(displaySpell.PlayerConditionId);

                if (playerCondition != null)
                    if (!ConditionManager.IsPlayerMeetingCondition(this, playerCondition))
                        continue;

                var spellInfo = SpellManager.GetSpellInfo(displaySpell.SpellId, Location.Map.DifficultyID);
                Unit caster = this;

                if (questGiver && questGiver.IsTypeMask(TypeMask.Unit) && !quest.HasFlag(QuestFlags.PlayerCastOnComplete) && !spellInfo.HasTargetType(Targets.UnitCaster))
                {
                    var unit = questGiver.AsUnit;

                    if (unit)
                        caster = unit;
                }

                caster.CastSpell(this, spellInfo.Id, new CastSpellExtraArgs(TriggerCastFlags.FullMask).SetCastDifficulty(spellInfo.Difficulty));
            }
        }

        if (quest.QuestSortID > 0)
            UpdateCriteria(CriteriaType.CompleteQuestsInZone, quest.Id);

        UpdateCriteria(CriteriaType.CompleteQuestsCount);
        UpdateCriteria(CriteriaType.CompleteQuest, quest.Id);
        UpdateCriteria(CriteriaType.CompleteAnyReplayQuest, 1);

        // make full db save
        SaveToDB();

        var questBit = DB2Manager.GetQuestUniqueBitFlag(questId);

        if (questBit != 0)
            SetQuestCompletedBit(questBit, true);

        if (quest.HasFlag(QuestFlags.Pvp))
        {
            PvpInfo.IsHostile = PvpInfo.IsInHostileArea || HasPvPForcingQuest();
            UpdatePvPState();
        }

        SendQuestGiverStatusMultiple();

        var conditionChanged = SendQuestUpdate(questId, false);

        //lets remove Id for delayed teleports
        SetCanDelayTeleport(false);

        if (questGiver != null)
        {
            var canHaveNextQuest = !quest.HasFlag(QuestFlags.AutoComplete) ? !questGiver.IsPlayer : true;

            if (canHaveNextQuest)
                switch (questGiver.TypeId)
                {
                    case TypeId.Unit:
                    case TypeId.Player:
                    {
                        //For AutoSubmition was added plr case there as it almost same exclute AI script cases.
                        // Send next quest
                        var nextQuest = GetNextQuest(questGiver.GUID, quest);

                        if (nextQuest != null)
                            // Only send the quest to the player if the conditions are met
                            if (CanTakeQuest(nextQuest, false))
                            {
                                if (nextQuest.IsAutoAccept && CanAddQuest(nextQuest, true))
                                    AddQuestAndCheckCompletion(nextQuest, questGiver);

                                PlayerTalkClass.SendQuestGiverQuestDetails(nextQuest, questGiver.GUID, true, false);
                            }

                        PlayerTalkClass.ClearMenus();
                        var creatureQGiver = questGiver.AsCreature;

                        creatureQGiver?.AI.OnQuestReward(this, quest, rewardType, rewardId);

                        break;
                    }
                    case TypeId.GameObject:
                    {
                        var questGiverGob = questGiver.AsGameObject;
                        // Send next quest
                        var nextQuest = GetNextQuest(questGiverGob.GUID, quest);

                        if (nextQuest != null)
                            // Only send the quest to the player if the conditions are met
                            if (CanTakeQuest(nextQuest, false))
                            {
                                if (nextQuest.IsAutoAccept && CanAddQuest(nextQuest, true))
                                    AddQuestAndCheckCompletion(nextQuest, questGiver);

                                PlayerTalkClass.SendQuestGiverQuestDetails(nextQuest, questGiverGob.GUID, true, false);
                            }

                        PlayerTalkClass.ClearMenus();
                        questGiverGob.AI.OnQuestReward(this, quest, rewardType, rewardId);

                        break;
                    }
                    default:
                        break;
                }
        }

        ScriptManager.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(this, questId));
        ScriptManager.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(this, quest, oldStatus, QuestStatus.Rewarded), quest.ScriptId);

        if (conditionChanged)
            UpdateObjectVisibility();
    }

    public void RewardQuestPackage(uint questPackageId, uint onlyItemId = 0)
    {
        var hasFilteredQuestPackageReward = false;
        var questPackageItems = DB2Manager.GetQuestPackageItems(questPackageId);

        if (questPackageItems != null)
            foreach (var questPackageItem in questPackageItems)
            {
                if (onlyItemId != 0 && questPackageItem.ItemID != onlyItemId)
                    continue;

                if (CanSelectQuestPackageItem(questPackageItem))
                {
                    hasFilteredQuestPackageReward = true;
                    List<ItemPosCount> dest = new();

                    if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity) == InventoryResult.Ok)
                    {
                        var item = StoreNewItem(dest, questPackageItem.ItemID, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(questPackageItem.ItemID));
                        SendNewItem(item, questPackageItem.ItemQuantity, true, false);
                    }
                }
            }

        if (!hasFilteredQuestPackageReward)
        {
            var questPackageItemsFallback = DB2Manager.GetQuestPackageItemsFallback(questPackageId);

            if (questPackageItemsFallback != null)
                foreach (var questPackageItem in questPackageItemsFallback)
                {
                    if (onlyItemId != 0 && questPackageItem.ItemID != onlyItemId)
                        continue;

                    List<ItemPosCount> dest = new();

                    if (CanStoreNewItem(ItemConst.NullBag, ItemConst.NullSlot, dest, questPackageItem.ItemID, questPackageItem.ItemQuantity) == InventoryResult.Ok)
                    {
                        var item = StoreNewItem(dest, questPackageItem.ItemID, true, ItemEnchantmentManager.GenerateItemRandomBonusListId(questPackageItem.ItemID));
                        SendNewItem(item, questPackageItem.ItemQuantity, true, false);
                    }
                }
        }
    }

    public bool SatisfyQuestClass(Quest.Quest qInfo, bool msg)
    {
        var reqClass = qInfo.AllowableClasses;

        if (reqClass == 0)
            return true;

        if ((reqClass & ClassMask) == 0)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.Logger.Debug("SatisfyQuestClass: Sent QuestFailedReason.None (questId: {0}) because player does not have required class.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestConditions(Quest.Quest qInfo, bool msg)
    {
        if (!ConditionManager.IsObjectMeetingNotGroupedConditions(ConditionSourceType.QuestAvailable, qInfo.Id, this))
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.Logger.Debug("SatisfyQuestConditions: Sent QuestFailedReason.None (questId: {0}) because player does not meet conditions.", qInfo.Id);
            }

            Log.Logger.Debug("SatisfyQuestConditions: conditions not met for quest {0}", qInfo.Id);

            return false;
        }

        return true;
    }

    public bool SatisfyQuestDay(Quest.Quest qInfo, bool msg)
    {
        if (!qInfo.IsDaily && !qInfo.IsDFQuest)
            return true;

        if (qInfo.IsDFQuest)
        {
            if (_dfQuests.Contains(qInfo.Id))
                return false;

            return true;
        }

        return ActivePlayerData.DailyQuestsCompleted.FindIndex(qInfo.Id) == -1;
    }

    public bool SatisfyQuestDependentQuests(Quest.Quest qInfo, bool msg)
    {
        return SatisfyQuestPreviousQuest(qInfo, msg) &&
               SatisfyQuestDependentPreviousQuests(qInfo, msg) &&
               SatisfyQuestBreadcrumbQuest(qInfo, msg) &&
               SatisfyQuestDependentBreadcrumbQuests(qInfo, msg);
    }

    public bool SatisfyQuestExclusiveGroup(Quest.Quest qInfo, bool msg)
    {
        // non positive exclusive group, if > 0 then can be start if any other quest in exclusive group already started/completed
        if (qInfo.ExclusiveGroup <= 0)
            return true;

        var range = Global.ObjectMgr.GetExclusiveQuestGroupBounds(qInfo.ExclusiveGroup);
        // always must be found if qInfo.ExclusiveGroup != 0

        foreach (var excludeId in range)
        {
            // skip checked quest id, only state of other quests in group is interesting
            if (excludeId == qInfo.Id)
                continue;

            // not allow have daily quest if daily quest from exclusive group already recently completed
            var nquest = Global.ObjectMgr.GetQuestTemplate(excludeId);

            if (!SatisfyQuestDay(nquest, false) || !SatisfyQuestWeek(nquest, false) || !SatisfyQuestSeasonal(nquest, false))
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.Logger.Debug("SatisfyQuestExclusiveGroup: Sent QuestFailedReason.None (questId: {0}) because player already did daily quests in exclusive group.", qInfo.Id);
                }

                return false;
            }

            // alternative quest already started or completed - but don't check rewarded states if both are repeatable
            if (GetQuestStatus(excludeId) != QuestStatus.None || (!(qInfo.IsRepeatable && nquest.IsRepeatable) && GetQuestRewardStatus(excludeId)))
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.Logger.Debug("SatisfyQuestExclusiveGroup: Sent QuestFailedReason.None (questId: {0}) because player already did quest in exclusive group.", qInfo.Id);
                }

                return false;
            }
        }

        return true;
    }

    public bool SatisfyQuestExpansion(Quest.Quest qInfo, bool msg)
    {
        if ((int)Session.Expansion < qInfo.Expansion)
        {
            if (msg)
                SendCanTakeQuestResponse(QuestFailedReasons.FailedExpansion);

            Log.Logger.Debug($"Player.SatisfyQuestExpansion: Sent QUEST_ERR_FAILED_EXPANSION (QuestID: {qInfo.Id}) because player '{GetName()}' ({GUID}) does not have required expansion.");

            return false;
        }

        return true;
    }

    public bool SatisfyQuestLog(bool msg)
    {
        // exist free slot
        if (FindQuestSlot(0) < SharedConst.MaxQuestLogSize)
            return true;

        if (msg)
            SendPacket(new QuestLogFull());

        return false;
    }

    public bool SatisfyQuestMaxLevel(Quest.Quest qInfo, bool msg)
    {
        if (qInfo.MaxLevel > 0 && Level > qInfo.MaxLevel)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None); // There doesn't seem to be a specific response for too high player level
                Log.Logger.Debug("SatisfyQuestMaxLevel: Sent QuestFailedReasons.None (questId: {0}) because player does not have required (max) level.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestMinLevel(Quest.Quest qInfo, bool msg)
    {
        if (Level < GetQuestMinLevel(qInfo))
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.FailedLowLevel);
                Log.Logger.Debug("SatisfyQuestMinLevel: Sent QuestFailedReasons.FailedLowLevel (questId: {0}) because player does not have required (min) level.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestMonth(Quest.Quest qInfo, bool msg)
    {
        if (!qInfo.IsMonthly || _monthlyquests.Empty())
            return true;

        // if not found in cooldown list
        return !_monthlyquests.Contains(qInfo.Id);
    }

    public bool SatisfyQuestPreviousQuest(Quest.Quest qInfo, bool msg)
    {
        // No previous quest (might be first quest in a series)
        if (qInfo.PrevQuestId == 0)
            return true;

        var prevId = (uint)Math.Abs(qInfo.PrevQuestId);

        // If positive previous quest rewarded, return true
        if (qInfo.PrevQuestId > 0 && _rewardedQuests.Contains(prevId))
            return true;

        // If negative previous quest active, return true
        if (qInfo.PrevQuestId < 0 && GetQuestStatus(prevId) == QuestStatus.Incomplete)
            return true;

        // Has positive prev. quest in non-rewarded state
        // and negative prev. quest in non-active state
        if (msg)
        {
            SendCanTakeQuestResponse(QuestFailedReasons.None);
            Log.Logger.Debug($"Player.SatisfyQuestPreviousQuest: Sent QUEST_ERR_NONE (QuestID: {qInfo.Id}) because player '{GetName()}' ({GUID}) doesn't have required quest {prevId}.");
        }

        return false;
    }

    public bool SatisfyQuestRace(Quest.Quest qInfo, bool msg)
    {
        var reqraces = qInfo.AllowableRaces;

        if (reqraces == -1)
            return true;

        if ((reqraces & SharedConst.GetMaskForRace(Race)) == 0)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.FailedWrongRace);
                Log.Logger.Debug("SatisfyQuestRace: Sent QuestFailedReasons.FailedWrongRace (questId: {0}) because player does not have required race.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestReputation(Quest.Quest qInfo, bool msg)
    {
        var fIdMin = qInfo.RequiredMinRepFaction; //Min required rep

        if (fIdMin != 0 && ReputationMgr.GetReputation(fIdMin) < qInfo.RequiredMinRepValue)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.Logger.Debug("SatisfyQuestReputation: Sent QuestFailedReason.None (questId: {0}) because player does not have required reputation (min).", qInfo.Id);
            }

            return false;
        }

        var fIdMax = qInfo.RequiredMaxRepFaction; //Max required rep

        if (fIdMax != 0 && ReputationMgr.GetReputation(fIdMax) >= qInfo.RequiredMaxRepValue)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.Logger.Debug("SatisfyQuestReputation: Sent QuestFailedReason.None (questId: {0}) because player does not have required reputation (max).", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestSeasonal(Quest.Quest qInfo, bool msg)
    {
        if (!qInfo.IsSeasonal || _seasonalquests.Empty())
            return true;

        var list = _seasonalquests.LookupByKey(qInfo.EventIdForQuest);

        if (list == null || list.Empty())
            return true;

        // if not found in cooldown list
        return !list.ContainsKey(qInfo.Id);
    }

    public bool SatisfyQuestSkill(Quest.Quest qInfo, bool msg)
    {
        var skill = qInfo.RequiredSkillId;

        // skip 0 case RequiredSkill
        if (skill == 0)
            return true;

        // check skill value
        if (GetSkillValue((SkillType)skill) < qInfo.RequiredSkillPoints)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.Logger.Debug("SatisfyQuestSkill: Sent QuestFailedReason.None (questId: {0}) because player does not have required skill value.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestStatus(Quest.Quest qInfo, bool msg)
    {
        if (GetQuestStatus(qInfo.Id) == QuestStatus.Rewarded)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.AlreadyDone);

                Log.Logger.Debug("Player.SatisfyQuestStatus: Sent QUEST_STATUS_REWARDED (QuestID: {0}) because player '{1}' ({2}) quest status is already REWARDED.",
                                 qInfo.Id,
                                 GetName(),
                                 GUID.ToString());
            }

            return false;
        }

        if (GetQuestStatus(qInfo.Id) != QuestStatus.None)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.AlreadyOn1);
                Log.Logger.Debug("SatisfyQuestStatus: Sent QuestFailedReasons.AlreadyOn1 (questId: {0}) because player quest status is not NONE.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestTimed(Quest.Quest qInfo, bool msg)
    {
        if (!_timedquests.Empty() && qInfo.LimitTime != 0)
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.OnlyOneTimed);
                Log.Logger.Debug("SatisfyQuestTimed: Sent QuestFailedReasons.OnlyOneTimed (questId: {0}) because player is already on a timed quest.", qInfo.Id);
            }

            return false;
        }

        return true;
    }

    public bool SatisfyQuestWeek(Quest.Quest qInfo, bool msg)
    {
        if (!qInfo.IsWeekly || _weeklyquests.Empty())
            return true;

        // if not found in cooldown list
        return !_weeklyquests.Contains(qInfo.Id);
    }

    public void SendCanTakeQuestResponse(QuestFailedReasons reason, bool sendErrorMessage = true, string reasonText = "")
    {
        QuestGiverInvalidQuest questGiverInvalidQuest = new()
        {
            Reason = reason,
            SendErrorMessage = sendErrorMessage,
            ReasonText = reasonText
        };

        SendPacket(questGiverInvalidQuest);
    }

    public void SendPreparedQuest(WorldObject source)
    {
        var questMenu = PlayerTalkClass.GetQuestMenu();

        if (questMenu.IsEmpty())
            return;

        // single element case
        if (questMenu.GetMenuItemCount() == 1)
        {
            var qmi0 = questMenu.GetItem(0);
            var questId = qmi0.QuestId;

            // Auto open
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (quest != null)
            {
                if (qmi0.QuestIcon == 4)
                {
                    PlayerTalkClass.SendQuestGiverRequestItems(quest, source.GUID, CanRewardQuest(quest, false), true);
                }

                // Send completable on repeatable and autoCompletable quest if player don't have quest
                // @todo verify if check for !quest.IsDaily() is really correct (possibly not)
                else if (!source.HasQuest(questId) && !source.HasInvolvedQuest(questId))
                {
                    PlayerTalkClass.SendCloseGossip();
                }
                else
                {
                    if (quest.IsAutoAccept && CanAddQuest(quest, true) && CanTakeQuest(quest, true))
                        AddQuestAndCheckCompletion(quest, source);

                    if (quest.IsAutoComplete && quest.IsRepeatable && !quest.IsDailyOrWeekly && !quest.IsMonthly)
                        PlayerTalkClass.SendQuestGiverRequestItems(quest, source.GUID, CanCompleteRepeatableQuest(quest), true);
                    else if (quest.IsAutoComplete && !quest.IsDailyOrWeekly && !quest.IsMonthly)
                        PlayerTalkClass.SendQuestGiverRequestItems(quest, source.GUID, CanRewardQuest(quest, false), true);
                    else
                        PlayerTalkClass.SendQuestGiverQuestDetails(quest, source.GUID, true, false);
                }

                return;
            }
        }

        PlayerTalkClass.SendQuestGiverQuestListMessage(source);
    }

    public void SendPushToPartyResponse(Player player, QuestPushReason reason, Quest.Quest quest = null)
    {
        if (player != null)
        {
            QuestPushResultResponse response = new()
            {
                SenderGUID = player.GUID,
                Result = reason
            };

            if (quest != null)
            {
                response.QuestTitle = quest.LogTitle;
                var localeConstant = Session.SessionDbLocaleIndex;

                if (localeConstant != Locale.enUS)
                {
                    var questTemplateLocale = Global.ObjectMgr.GetQuestLocale(quest.Id);

                    if (questTemplateLocale != null)
                        GameObjectManager.GetLocaleString(questTemplateLocale.LogTitle, localeConstant, ref response.QuestTitle);
                }
            }

            SendPacket(response);
        }
    }

    public void SendQuestComplete(uint questId)
    {
        if (questId != 0)
        {
            QuestUpdateComplete data = new()
            {
                QuestID = questId
            };

            SendPacket(data);
        }
    }

    public void SendQuestConfirmAccept(Quest.Quest quest, Player receiver)
    {
        if (!receiver)
            return;

        QuestConfirmAcceptResponse packet = new()
        {
            QuestTitle = quest.LogTitle
        };

        var locIdx = receiver.Session.SessionDbLocaleIndex;

        if (locIdx != Locale.enUS)
        {
            var questLocale = Global.ObjectMgr.GetQuestLocale(quest.Id);

            if (questLocale != null)
                GameObjectManager.GetLocaleString(questLocale.LogTitle, locIdx, ref packet.QuestTitle);
        }

        packet.QuestID = quest.Id;
        packet.InitiatedBy = GUID;

        receiver.SendPacket(packet);
    }

    public void SendQuestFailed(uint questId, InventoryResult reason = InventoryResult.Ok)
    {
        if (questId != 0)
        {
            QuestGiverQuestFailed questGiverQuestFailed = new()
            {
                QuestID = questId,
                Reason = reason // failed reason (valid reasons: 4, 16, 50, 17, other values show default message)
            };

            SendPacket(questGiverQuestFailed);
        }
    }

    public void SendQuestGiverStatusMultiple()
    {
        lock (ClientGuiDs)
        {
            SendQuestGiverStatusMultiple(ClientGuiDs);
        }
    }

    public void SendQuestGiverStatusMultiple(List<ObjectGuid> guids)
    {
        QuestGiverStatusMultiple response = new();

        foreach (var itr in guids)
            if (itr.IsAnyTypeCreature)
            {
                // need also pet quests case support
                var questgiver = ObjectAccessor.GetCreatureOrPetOrVehicle(this, itr);

                if (!questgiver || questgiver.WorldObjectCombat.IsHostileTo(this))
                    continue;

                if (!questgiver.HasNpcFlag(NPCFlags.QuestGiver))
                    continue;

                response.QuestGiver.Add(new QuestGiverInfo(questgiver.GUID, GetQuestDialogStatus(questgiver)));
            }
            else if (itr.IsGameObject)
            {
                var questgiver = Location.Map.GetGameObject(itr);

                if (!questgiver || questgiver.GoType != GameObjectTypes.QuestGiver)
                    continue;

                response.QuestGiver.Add(new QuestGiverInfo(questgiver.GUID, GetQuestDialogStatus(questgiver)));
            }

        SendPacket(response);
    }

    public void SendQuestReward(Quest.Quest quest, Creature questGiver, uint xp, bool hideChatMessage)
    {
        var questId = quest.Id;
        Global.GameEventMgr.HandleQuestComplete(questId);

        uint moneyReward;

        if (!IsMaxLevel)
        {
            moneyReward = GetQuestMoneyReward(quest);
        }
        else // At max level, increase gold reward
        {
            xp = 0;
            moneyReward = (uint)(GetQuestMoneyReward(quest) + (int)(quest.GetRewMoneyMaxLevel() * GetDefaultValue("Rate.Drop.Money", 1.0f)));
        }

        QuestGiverQuestComplete packet = new()
        {
            QuestID = questId,
            MoneyReward = moneyReward,
            XPReward = xp,
            SkillLineIDReward = quest.RewardSkillId,
            NumSkillUpsReward = quest.RewardSkillPoints
        };

        if (questGiver)
        {
            if (questGiver.IsGossip)
            {
                packet.LaunchGossip = true;
            }
            else if (questGiver.IsQuestGiver)
            {
                packet.LaunchQuest = true;
            }
            else if (quest.NextQuestInChain != 0 && !quest.HasFlag(QuestFlags.AutoComplete))
            {
                var rewardQuest = Global.ObjectMgr.GetQuestTemplate(quest.NextQuestInChain);

                if (rewardQuest != null)
                    packet.UseQuestReward = CanTakeQuest(rewardQuest, false);
            }
        }

        packet.HideChatMessage = hideChatMessage;

        SendPacket(packet);
    }

    public void SendQuestTimerFailed(uint questId)
    {
        if (questId != 0)
        {
            QuestUpdateFailedTimer questUpdateFailedTimer = new()
            {
                QuestID = questId
            };

            SendPacket(questUpdateFailedTimer);
        }
    }

    public void SendQuestUpdateAddCreditSimple(QuestObjective obj)
    {
        QuestUpdateAddCreditSimple packet = new()
        {
            QuestID = obj.QuestID,
            ObjectID = obj.ObjectID,
            ObjectiveType = obj.Type
        };

        SendPacket(packet);
    }

    public void SendQuestUpdateAddPlayer(Quest.Quest quest, uint newCount)
    {
        QuestUpdateAddPvPCredit packet = new()
        {
            QuestID = quest.Id,
            Count = (ushort)newCount
        };

        SendPacket(packet);
    }

    public void SetInGameTime(uint time)
    {
        _ingametime = time;
    }

    public void SetQuestObjectiveData(QuestObjective objective, int data)
    {
        if (objective.StorageIndex < 0)
        {
            Log.Logger.Error($"Player.SetQuestObjectiveData: called for quest {objective.QuestID} with invalid StorageIndex {objective.StorageIndex} (objective data is not tracked)");

            return;
        }

        var status = _mQuestStatus.LookupByKey(objective.QuestID);

        if (status == null)
        {
            Log.Logger.Error($"Player.SetQuestObjectiveData: player '{GetName()}' ({GUID}) doesn't have quest status data (QuestID: {objective.QuestID})");

            return;
        }

        if (objective.StorageIndex >= SharedConst.MaxQuestCounts)
        {
            Log.Logger.Error($"Player.SetQuestObjectiveData: player '{GetName()}' ({GUID}) quest {objective.QuestID} out of range StorageIndex {objective.StorageIndex}");

            return;
        }

        if (status.Slot >= SharedConst.MaxQuestLogSize)
            return;

        // No change
        var oldData = GetQuestSlotObjectiveData(status.Slot, objective);

        if (oldData == data)
            return;

        var quest = Global.ObjectMgr.GetQuestTemplate(objective.QuestID);

        if (quest != null)
            ScriptManager.RunScript<IQuestOnQuestObjectiveChange>(script => script.OnQuestObjectiveChange(this, quest, objective, oldData, data), quest.ScriptId);

        // Add to save
        _questStatusSave[objective.QuestID] = QuestSaveType.Default;

        // Update quest fields
        if (!objective.IsStoringFlag())
            SetQuestSlotCounter(status.Slot, (byte)objective.StorageIndex, (ushort)data);
        else if (data != 0)
            SetQuestSlotObjectiveFlag(status.Slot, objective.StorageIndex);
        else
            RemoveQuestSlotObjectiveFlag(status.Slot, objective.StorageIndex);
    }

    public void SetQuestSharingInfo(ObjectGuid guid, uint id)
    {
        _playerSharingQuest = guid;
        _sharedQuestId = id;
    }
    public void SetQuestSlot(ushort slot, uint questID)
    {
        var questLogField = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        SetUpdateFieldValue(questLogField.ModifyValue(questLogField.QuestID), questID);
        SetUpdateFieldValue(questLogField.ModifyValue(questLogField.StateFlags), 0u);
        SetUpdateFieldValue(questLogField.ModifyValue(questLogField.EndTime), 0u);
        SetUpdateFieldValue(questLogField.ModifyValue(questLogField.AcceptTime), 0u);
        SetUpdateFieldValue(questLogField.ModifyValue(questLogField.ObjectiveFlags), 0u);

        for (var i = 0; i < SharedConst.MaxQuestCounts; ++i)
            SetUpdateFieldValue(ref questLogField.ModifyValue(questLogField.ObjectiveProgress, i), (ushort)0);
    }

    public void SetQuestSlotAcceptTime(ushort slot, long acceptTime)
    {
        var questLog = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        SetUpdateFieldValue(questLog.ModifyValue(questLog.AcceptTime), (uint)acceptTime);
    }

    public void SetQuestSlotCounter(ushort slot, byte counter, ushort count)
    {
        if (counter >= SharedConst.MaxQuestCounts)
            return;

        var questLog = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        SetUpdateFieldValue(ref questLog.ModifyValue(questLog.ObjectiveProgress, counter), count);
    }

    public void SetQuestSlotEndTime(ushort slot, long endTime)
    {
        var questLog = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        SetUpdateFieldValue(questLog.ModifyValue(questLog.EndTime), (uint)endTime);
    }

    public void SetQuestSlotState(ushort slot, QuestSlotStateMask state)
    {
        var questLogField = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        SetUpdateFieldFlagValue(questLogField.ModifyValue(questLogField.StateFlags), (uint)state);
    }

    public void SetQuestStatus(uint questId, QuestStatus status, bool update = true)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(questId);

        if (quest != null)
        {
            if (!_mQuestStatus.ContainsKey(questId))
                _mQuestStatus[questId] = new QuestStatusData();

            var oldStatus = _mQuestStatus[questId].Status;
            _mQuestStatus[questId].Status = status;

            if (!quest.IsAutoComplete)
                _questStatusSave[questId] = QuestSaveType.Default;

            ScriptManager.ForEach<IPlayerOnQuestStatusChange>(p => p.OnQuestStatusChange(this, questId));
            ScriptManager.RunScript<IQuestOnQuestStatusChange>(script => script.OnQuestStatusChange(this, quest, oldStatus, status), quest.ScriptId);
        }

        if (update)
            SendQuestUpdate(questId);
    }

    public void SetRewardedQuest(uint questID)
    {
        _rewardedQuests.Add(questID);
        _rewardedQuestsSave[questID] = QuestSaveType.Default;
    }
    public bool TakeQuestSourceItem(uint questId, bool msg)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(questId);

        if (quest != null)
        {
            var srcItemId = quest.SourceItemId;
            var item = Global.ObjectMgr.GetItemTemplate(srcItemId);

            if (srcItemId > 0)
            {
                var count = quest.SourceItemIdCount;

                if (count <= 0)
                    count = 1;

                // There are two cases where the source item is not destroyed:
                // - Item cannot be unequipped (example: non-empty bags)
                // - The source item is the item that started the quest, so the player is supposed to keep it (otherwise it was already destroyed in AddQuestAndCheckCompletion())
                var res = CanUnequipItems(srcItemId, count);

                if (res != InventoryResult.Ok)
                {
                    if (msg)
                        SendEquipError(res, null, null, srcItemId);

                    return false;
                }

                if (item.StartQuest != questId)
                    DestroyItemCount(srcItemId, count, true);
            }
        }

        return true;
    }
    public void TalkedToCreature(uint entry, ObjectGuid guid)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.TalkTo, (int)entry, 1, guid);
    }
    public void UpdateQuestObjectiveProgress(QuestObjectiveType objectiveType, int objectId, long addCount, ObjectGuid victimGuid = default)
    {
        var anyObjectiveChangedCompletionState = false;

        foreach (var objectiveStatusData in _questObjectiveStatus.LookupByKey((objectiveType, objectId)))
        {
            var questId = objectiveStatusData.QuestStatusPair.QuestID;
            var quest = Global.ObjectMgr.GetQuestTemplate(questId);

            if (!QuestObjective.CanAlwaysBeProgressedInRaid(objectiveType))
                if (Group && Group.IsRaidGroup && quest.IsAllowedInRaid(Location.Map.DifficultyID))
                    continue;

            var logSlot = objectiveStatusData.QuestStatusPair.Status.Slot;
            var objective = objectiveStatusData.Objective;

            if (!IsQuestObjectiveCompletable(logSlot, quest, objective))
                continue;

            var objectiveWasComplete = IsQuestObjectiveComplete(logSlot, quest, objective);

            if (!objectiveWasComplete || addCount < 0)
            {
                var objectiveIsNowComplete = false;

                if (objective.IsStoringValue())
                {
                    if (objectiveType == QuestObjectiveType.PlayerKills && objective.Flags.HasAnyFlag(QuestObjectiveFlags.KillPlayersSameFaction))
                    {
                        var victim = Global.ObjAccessor.GetPlayer(Location.Map, victimGuid);

                        if (victim?.EffectiveTeam != EffectiveTeam)
                            continue;
                    }

                    var currentProgress = GetQuestSlotObjectiveData(logSlot, objective);

                    if (addCount > 0 ? (currentProgress < objective.Amount) : (currentProgress > 0))
                    {
                        var newProgress = (int)Math.Clamp(currentProgress + addCount, 0, objective.Amount);
                        SetQuestObjectiveData(objective, newProgress);

                        if (addCount > 0 && !objective.Flags.HasAnyFlag(QuestObjectiveFlags.HideCreditMsg))
                            switch (objectiveType)
                            {
                                case QuestObjectiveType.Item:
                                    break;
                                case QuestObjectiveType.PlayerKills:
                                    SendQuestUpdateAddPlayer(quest, (uint)newProgress);

                                    break;
                                default:
                                    SendQuestUpdateAddCredit(quest, victimGuid, objective, (uint)newProgress);

                                    break;
                            }

                        objectiveIsNowComplete = IsQuestObjectiveComplete(logSlot, quest, objective);
                    }
                }
                else if (objective.IsStoringFlag())
                {
                    SetQuestObjectiveData(objective, addCount > 0 ? 1 : 0);

                    if (addCount > 0 && !objective.Flags.HasAnyFlag(QuestObjectiveFlags.HideCreditMsg))
                        SendQuestUpdateAddCreditSimple(objective);

                    objectiveIsNowComplete = IsQuestObjectiveComplete(logSlot, quest, objective);
                }
                else
                {
                    switch (objectiveType)
                    {
                        case QuestObjectiveType.Currency:
                            objectiveIsNowComplete = GetCurrencyQuantity((uint)objectId) + addCount >= objective.Amount;

                            break;
                        case QuestObjectiveType.LearnSpell:
                            objectiveIsNowComplete = addCount != 0;

                            break;
                        case QuestObjectiveType.MinReputation:
                            objectiveIsNowComplete = ReputationMgr.GetReputation((uint)objectId) + addCount >= objective.Amount;

                            break;
                        case QuestObjectiveType.MaxReputation:
                            objectiveIsNowComplete = ReputationMgr.GetReputation((uint)objectId) + addCount <= objective.Amount;

                            break;
                        case QuestObjectiveType.Money:
                            objectiveIsNowComplete = (long)Money + addCount >= objective.Amount;

                            break;
                        case QuestObjectiveType.ProgressBar:
                            objectiveIsNowComplete = IsQuestObjectiveProgressBarComplete(logSlot, quest);

                            break;
                    }
                }

                if (objective.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar))
                    if (IsQuestObjectiveProgressBarComplete(logSlot, quest))
                    {
                        var progressBarObjective = quest.Objectives.Find(otherObjective => otherObjective.Type == QuestObjectiveType.ProgressBar && !otherObjective.Flags.HasAnyFlag(QuestObjectiveFlags.PartOfProgressBar));

                        if (progressBarObjective != null)
                            SendQuestUpdateAddCreditSimple(progressBarObjective);

                        objectiveIsNowComplete = true;
                    }

                if (objectiveWasComplete != objectiveIsNowComplete)
                    anyObjectiveChangedCompletionState = true;

                if (objectiveIsNowComplete && CanCompleteQuest(questId, objective.Id))
                    CompleteQuest(questId);
                else if (objectiveStatusData.QuestStatusPair.Status.Status == QuestStatus.Complete)
                    IncompleteQuest(questId);
            }
        }

        if (anyObjectiveChangedCompletionState)
            UpdateVisibleGameobjectsOrSpellClicks();

        PhasingHandler.OnConditionChange(this);
    }
    public void UpdateVisibleGameobjectsOrSpellClicks()
    {
        if (ClientGuiDs.Empty())
            return;

        UpdateData udata = new(Location.MapId);

        lock (ClientGuiDs)
        {
            foreach (var guid in ClientGuiDs)
                if (guid.IsGameObject)
                {
                    var obj = ObjectAccessor.GetGameObject(this, guid);

                    if (obj != null)
                    {
                        ObjectFieldData objMask = new();
                        GameObjectFieldData goMask = new();

                        if (_questObjectiveStatus.ContainsKey((QuestObjectiveType.GameObject, (int)obj.Entry)))
                            objMask.MarkChanged(obj.ObjectData.DynamicFlags);

                        switch (obj.GoType)
                        {
                            case GameObjectTypes.QuestGiver:
                            case GameObjectTypes.Chest:
                            case GameObjectTypes.Goober:
                            case GameObjectTypes.Generic:
                            case GameObjectTypes.GatheringNode:
                                if (Global.ObjectMgr.IsGameObjectForQuests(obj.Entry))
                                    objMask.MarkChanged(obj.ObjectData.DynamicFlags);

                                break;
                            default:
                                break;
                        }

                        if (objMask.GetUpdateMask().IsAnySet() || goMask.GetUpdateMask().IsAnySet())
                            obj.BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), goMask.GetUpdateMask(), this);
                    }
                }
                else if (guid.IsCreatureOrVehicle)
                {
                    var obj = ObjectAccessor.GetCreatureOrPetOrVehicle(this, guid);

                    if (obj == null)
                        continue;

                    // check if this unit requires quest specific flags
                    if (!obj.HasNpcFlag(NPCFlags.SpellClick))
                        continue;

                    var clickBounds = Global.ObjectMgr.GetSpellClickInfoMapBounds(obj.Entry);

                    foreach (var spellClickInfo in clickBounds)
                    {
                        var conds = ConditionManager.GetConditionsForSpellClickEvent(obj.Entry, spellClickInfo.spellId);

                        if (conds != null)
                        {
                            ObjectFieldData objMask = new();
                            UnitData unitMask = new();
                            unitMask.MarkChanged(UnitData.NpcFlags, 0); // NpcFlags[0] has UNIT_NPC_FLAG_SPELLCLICK
                            obj.BuildValuesUpdateForPlayerWithMask(udata, objMask.GetUpdateMask(), unitMask.GetUpdateMask(), this);

                            break;
                        }
                    }
                }
        }

        udata.BuildPacket(out var packet);
        SendPacket(packet);
    }
    private void AddTimedQuest(uint questId)
    {
        _timedquests.Add(questId);
    }

    private void CurrencyChanged(uint currencyId, int change)
    {
        UpdateQuestObjectiveProgress(QuestObjectiveType.Currency, (int)currencyId, change);
        UpdateQuestObjectiveProgress(QuestObjectiveType.HaveCurrency, (int)currencyId, change);
        UpdateQuestObjectiveProgress(QuestObjectiveType.ObtainCurrency, (int)currencyId, change);
    }

    private uint GetInGameTime()
    {
        return _ingametime;
    }
    private int GetQuestMinLevel(uint contentTuningId)
    {
        var questLevels = DB2Manager.GetContentTuningData(contentTuningId, PlayerData.CtrOptions.Value.ContentTuningConditionMask);

        if (questLevels.HasValue)
        {
            var race = CliDB.ChrRacesStorage.LookupByKey(Race);
            var raceFaction = CliDB.FactionTemplateStorage.LookupByKey(race.FactionID);
            var questFactionGroup = CliDB.ContentTuningStorage.LookupByKey(contentTuningId).GetScalingFactionGroup();

            if (questFactionGroup != 0 && raceFaction.FactionGroup != questFactionGroup)
                return questLevels.Value.MaxLevel;

            return questLevels.Value.MinLevelWithDelta;
        }

        return 0;
    }

    private bool GetQuestSlotObjectiveFlag(ushort slot, sbyte objectiveIndex)
    {
        if (objectiveIndex < SharedConst.MaxQuestCounts)
            return ((PlayerData.QuestLog[slot].ObjectiveFlags) & (1 << objectiveIndex)) != 0;

        return false;
    }

    private Dictionary<uint, QuestStatusData> GetQuestStatusMap()
    {
        return _mQuestStatus;
    }
    private void PushQuests()
    {
        foreach (var quest in Global.ObjectMgr.GetQuestTemplatesAutoPush())
        {
            if (quest.QuestTag != 0 && quest.QuestTag != QuestTagType.Tag)
                continue;

            if (!quest.IsUnavailable && CanTakeQuest(quest, false))
                AddQuestAndCheckCompletion(quest, null);
        }
    }

    private void RemoveQuestSlotObjectiveFlag(ushort slot, sbyte objectiveIndex)
    {
        var questLog = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        RemoveUpdateFieldFlagValue(questLog.ModifyValue(questLog.ObjectiveFlags), 1u << objectiveIndex);
    }

    private bool SatisfyQuestBreadcrumbQuest(Quest.Quest qInfo, bool msg)
    {
        var breadcrumbTargetQuestId = (uint)Math.Abs(qInfo.BreadcrumbForQuestId);

        //If this is not a breadcrumb quest.
        if (breadcrumbTargetQuestId == 0)
            return true;

        // If the target quest is not available
        if (!CanTakeQuest(Global.ObjectMgr.GetQuestTemplate(breadcrumbTargetQuestId), false))
        {
            if (msg)
            {
                SendCanTakeQuestResponse(QuestFailedReasons.None);
                Log.Logger.Debug($"Player.SatisfyQuestBreadcrumbQuest: Sent INVALIDREASON_DONT_HAVE_REQ (QuestID: {qInfo.Id}) because target quest (QuestID: {breadcrumbTargetQuestId}) is not available to player '{GetName()}' ({GUID}).");
            }

            return false;
        }

        return true;
    }

    private bool SatisfyQuestDependentBreadcrumbQuests(Quest.Quest qInfo, bool msg)
    {
        foreach (var breadcrumbQuestId in qInfo.DependentBreadcrumbQuests)
        {
            var status = GetQuestStatus(breadcrumbQuestId);

            // If any of the breadcrumb quests are in the quest log, return false.
            if (status == QuestStatus.Incomplete || status == QuestStatus.Complete || status == QuestStatus.Failed)
            {
                if (msg)
                {
                    SendCanTakeQuestResponse(QuestFailedReasons.None);
                    Log.Logger.Debug($"Player.SatisfyQuestDependentBreadcrumbQuests: Sent INVALIDREASON_DONT_HAVE_REQ (QuestID: {qInfo.Id}) because player '{GetName()}' ({GUID}) has a breadcrumb quest towards this quest in the quest log.");
                }

                return false;
            }
        }

        return true;
    }

    private bool SatisfyQuestDependentPreviousQuests(Quest.Quest qInfo, bool msg)
    {
        // No previous quest (might be first quest in a series)
        if (qInfo.DependentPreviousQuests.Empty())
            return true;

        foreach (var prevId in qInfo.DependentPreviousQuests)
        {
            // checked in startup
            var questInfo = Global.ObjectMgr.GetQuestTemplate(prevId);

            // If any of the previous quests completed, return true
            if (IsQuestRewarded(prevId))
            {
                // skip one-from-all exclusive group
                if (questInfo.ExclusiveGroup >= 0)
                    return true;

                // each-from-all exclusive group (< 0)
                // can be start if only all quests in prev quest exclusive group completed and rewarded
                var bounds = Global.ObjectMgr.GetExclusiveQuestGroupBounds(questInfo.ExclusiveGroup);

                foreach (var exclusiveQuestId in bounds)
                {
                    // skip checked quest id, only state of other quests in group is interesting
                    if (exclusiveQuestId == prevId)
                        continue;

                    // alternative quest from group also must be completed and rewarded (reported)
                    if (!IsQuestRewarded(exclusiveQuestId))
                    {
                        if (msg)
                        {
                            SendCanTakeQuestResponse(QuestFailedReasons.None);
                            Log.Logger.Debug($"Player.SatisfyQuestDependentPreviousQuests: Sent QUEST_ERR_NONE (QuestID: {qInfo.Id}) because player '{GetName()}' ({GUID}) doesn't have the required quest (1).");
                        }

                        return false;
                    }
                }

                return true;
            }
        }

        // Has only prev. quests in non-rewarded state
        if (msg)
        {
            SendCanTakeQuestResponse(QuestFailedReasons.None);
            Log.Logger.Debug($"Player.SatisfyQuestDependentPreviousQuests: Sent QUEST_ERR_NONE (QuestID: {qInfo.Id}) because player '{GetName()}' ({GUID}) doesn't have required quest (2).");
        }

        return false;
    }

    private bool SatisfyQuestLevel(Quest.Quest qInfo, bool msg)
    {
        return SatisfyQuestMinLevel(qInfo, msg) && SatisfyQuestMaxLevel(qInfo, msg);
    }
    private void SendDisplayToast(uint entry, DisplayToastType type, bool isBonusRoll, uint quantity, DisplayToastMethod method, uint questId, Item item = null)
    {
        DisplayToast displayToast = new()
        {
            Quantity = quantity,
            DisplayToastMethod = method,
            QuestID = questId,
            Type = type
        };

        switch (type)
        {
            case DisplayToastType.NewItem:
            {
                if (!item)
                    return;

                displayToast.BonusRoll = isBonusRoll;
                displayToast.Item = new ItemInstance(item);
                displayToast.LootSpec = 0; // loot spec that was selected when loot was generated (not at loot time)
                displayToast.Gender = NativeGender;

                break;
            }
            case DisplayToastType.NewCurrency:
                displayToast.CurrencyID = entry;

                break;
            default:
                break;
        }

        SendPacket(displayToast);
    }

    private bool SendQuestUpdate(uint questId, bool updateVisiblity = true)
    {
        var saBounds = SpellManager.GetSpellAreaForQuestMapBounds(questId);

        if (!saBounds.Empty())
        {
            List<uint> aurasToRemove = new();
            List<uint> aurasToCast = new();

            foreach (var spell in saBounds)
                if (spell.Flags.HasAnyFlag(SpellAreaFlag.AutoRemove) && !spell.IsFitToRequirements(this, Location.Zone, Location.Area))
                    aurasToRemove.Add(spell.SpellId);
                else if (spell.Flags.HasAnyFlag(SpellAreaFlag.AutoCast) && !spell.Flags.HasAnyFlag(SpellAreaFlag.IgnoreAutocastOnQuestStatusChange))
                    aurasToCast.Add(spell.SpellId);

            // Auras matching the requirements will be inside the aurasToCast container.
            // Auras not matching the requirements may prevent using auras matching the requirements.
            // aurasToCast will erase conflicting auras in aurasToRemove container to handle spells used by multiple quests.

            for (var c = 0; c < aurasToRemove.Count;)
            {
                var auraRemoved = false;

                foreach (var i in aurasToCast)
                    if (aurasToRemove[c] == i)
                    {
                        aurasToRemove.Remove(aurasToRemove[c]);
                        auraRemoved = true;

                        break;
                    }

                if (!auraRemoved)
                    ++c;
            }

            foreach (var spellId in aurasToCast)
                if (!HasAura(spellId))
                    SpellFactory.CastSpell(this, spellId, true);

            foreach (var spellId in aurasToRemove)
                RemoveAura(spellId);
        }

        UpdateVisibleGameobjectsOrSpellClicks();

        return PhasingHandler.OnConditionChange(this, updateVisiblity);
    }
    private void SendQuestUpdateAddCredit(Quest.Quest quest, ObjectGuid guid, QuestObjective obj, uint count)
    {
        QuestUpdateAddCredit packet = new()
        {
            VictimGUID = guid,
            QuestID = quest.Id,
            ObjectID = obj.ObjectID,
            Count = (ushort)count,
            Required = (ushort)obj.Amount,
            ObjectiveType = (byte)obj.Type
        };

        SendPacket(packet);
    }

    private void SetDailyQuestStatus(uint questID)
    {
        var qQuest = Global.ObjectMgr.GetQuestTemplate(questID);

        if (qQuest != null)
        {
            if (!qQuest.IsDFQuest)
            {
                AddDynamicUpdateFieldValue(Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.DailyQuestsCompleted), questID);
                _lastDailyQuestTime = GameTime.CurrentTime; // last daily quest time
                _dailyQuestChanged = true;
            }
            else
            {
                _dfQuests.Add(questID);
                _lastDailyQuestTime = GameTime.CurrentTime;
                _dailyQuestChanged = true;
            }
        }
    }

    private void SetMonthlyQuestStatus(uint questID)
    {
        _monthlyquests.Add(questID);
        _monthlyQuestChanged = true;
    }

    private void SetQuestCompletedBit(uint questBit, bool completed)
    {
        if (questBit == 0)
            return;

        var fieldOffset = (uint)((questBit - 1) / ActivePlayerData.QuestCompletedBitsPerBlock);

        if (fieldOffset >= ActivePlayerData.QuestCompletedBitsSize)
            return;

        var flag = 1ul << (((int)questBit - 1) % ActivePlayerData.QuestCompletedBitsPerBlock);

        if (completed)
            SetUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.QuestCompleted, (int)fieldOffset), flag);
        else
            RemoveUpdateFieldFlagValue(ref Values.ModifyValue(ActivePlayerData).ModifyValue(ActivePlayerData.QuestCompleted, (int)fieldOffset), flag);
    }

    private void SetQuestSlotObjectiveFlag(ushort slot, sbyte objectiveIndex)
    {
        var questLog = Values.ModifyValue(PlayerData).ModifyValue(PlayerData.QuestLog, slot);
        SetUpdateFieldFlagValue(questLog.ModifyValue(questLog.ObjectiveFlags), 1u << objectiveIndex);
    }
    private void SetSeasonalQuestStatus(uint questID)
    {
        var quest = Global.ObjectMgr.GetQuestTemplate(questID);

        if (quest == null)
            return;

        if (!_seasonalquests.ContainsKey(quest.EventIdForQuest))
            _seasonalquests[quest.EventIdForQuest] = new Dictionary<uint, long>();

        _seasonalquests[quest.EventIdForQuest][questID] = GameTime.CurrentTime;
        _seasonalQuestChanged = true;
    }

    private void SetWeeklyQuestStatus(uint questID)
    {
        _weeklyquests.Add(questID);
        _weeklyQuestChanged = true;
    }
}