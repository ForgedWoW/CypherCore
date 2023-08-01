// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.A;
using Forged.MapServer.DataStorage.Structs.B;
using Forged.MapServer.DataStorage.Structs.C;
using Forged.MapServer.DataStorage.Structs.F;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.DataStorage.Structs.P;
using Forged.MapServer.DataStorage.Structs.Q;
using Forged.MapServer.DataStorage.Structs.S;
using Forged.MapServer.Entities.AreaTriggers;
using Forged.MapServer.Questing;
using Forged.MapServer.Spells;
using Forged.MapServer.World;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Game.Common;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Globals.Caching;

public class QuestTemplateCache : IObjectCache
{
    private readonly DB6Storage<AreaTableRecord> _areaTableRecords;
    private readonly AreaTriggerDataStorage _areaTriggerDataStorage;
    private readonly DB6Storage<AreaTriggerRecord> _areaTriggerRecords;
    private readonly DB6Storage<BattlePetSpeciesRecord> _battlePetSpeciesRecords;
    private readonly DB6Storage<CharTitlesRecord> _charTitlesRecords;
    private readonly ClassFactory _classFactory;
    private readonly IConfiguration _configuration;
    private readonly DB6Storage<ContentTuningRecord> _contentTuningRecords;
    private readonly CreatureTemplateCache _creatureTemplateCache;
    private readonly DB6Storage<CriteriaTreeRecord> _criteriaTreeRecords;
    private readonly DB6Storage<CurrencyTypesRecord> _currencyTypesRecords;
    private readonly DisableManager _disableManager;
    private readonly MultiMap<int, uint> _exclusiveQuestGroups = new();
    private readonly DB6Storage<FactionRecord> _factionRecords;
    private readonly GameObjectTemplateCache _gameObjectTemplateCache;
    private readonly ItemTemplateCache _itemTemplateCache;
    private readonly DB6Storage<MailTemplateRecord> _mailTemplateRecords;
    private readonly DB6Storage<ParagonReputationRecord> _paragonReputationRecords;
    private readonly Dictionary<uint, QuestObjective> _questObjectives = new();
    private readonly DB6Storage<QuestSortRecord> _questSortRecords;
    private readonly DB6Storage<SkillLineRecord> _skillLineRecords;
    private readonly DB6Storage<SoundKitRecord> _soundKitRecords;
    private readonly SpellManager _spellManager;
    private readonly DB6Storage<SpellNameRecord> _spellNameRecords;
    private readonly WorldDatabase _worldDatabase;
    private readonly WorldManager _worldManager;

    public QuestTemplateCache(WorldDatabase worldDatabase, ClassFactory classFactory, DisableManager disableManager, ItemTemplateCache itemTemplateCache,
                              IConfiguration configuration, SpellManager spellManager, CreatureTemplateCache creatureTemplateCache, GameObjectTemplateCache gameObjectTemplateCache,
                              AreaTriggerDataStorage areaTriggerDataStorage, WorldManager worldManager, DB6Storage<ContentTuningRecord> contentTuningRecords,
                              DB6Storage<AreaTableRecord> areaTableRecords, DB6Storage<QuestSortRecord> questSortRecords, DB6Storage<SkillLineRecord> skillLineRecords,
                              DB6Storage<FactionRecord> factionRecords, DB6Storage<CharTitlesRecord> charTitlesRecords, DB6Storage<CurrencyTypesRecord> currencyTypesRecords,
                              DB6Storage<BattlePetSpeciesRecord> battlePetSpeciesRecords, DB6Storage<CriteriaTreeRecord> criteriaTreeRecords, DB6Storage<AreaTriggerRecord> areaTriggerRecords,
                              DB6Storage<MailTemplateRecord> mailTemplateRecords, DB6Storage<SoundKitRecord> soundKitRecords, DB6Storage<SpellNameRecord> spellNameRecords,
                              DB6Storage<ParagonReputationRecord> paragonReputationRecords)
    {
        _worldDatabase = worldDatabase;
        _classFactory = classFactory;
        _disableManager = disableManager;
        _itemTemplateCache = itemTemplateCache;
        _configuration = configuration;
        _spellManager = spellManager;
        _creatureTemplateCache = creatureTemplateCache;
        _gameObjectTemplateCache = gameObjectTemplateCache;
        _areaTriggerDataStorage = areaTriggerDataStorage;
        _worldManager = worldManager;
        _contentTuningRecords = contentTuningRecords;
        _areaTableRecords = areaTableRecords;
        _questSortRecords = questSortRecords;
        _skillLineRecords = skillLineRecords;
        _factionRecords = factionRecords;
        _charTitlesRecords = charTitlesRecords;
        _currencyTypesRecords = currencyTypesRecords;
        _battlePetSpeciesRecords = battlePetSpeciesRecords;
        _criteriaTreeRecords = criteriaTreeRecords;
        _areaTriggerRecords = areaTriggerRecords;
        _mailTemplateRecords = mailTemplateRecords;
        _soundKitRecords = soundKitRecords;
        _spellNameRecords = spellNameRecords;
        _paragonReputationRecords = paragonReputationRecords;
    }

    public Dictionary<uint, Quest> QuestTemplates { get; } = new();
    public List<Quest> QuestTemplatesAutoPush { get; } = new();

    public List<uint> GetExclusiveQuestGroupBounds(int exclusiveGroupId)
    {
        return _exclusiveQuestGroups.LookupByKey(exclusiveGroupId);
    }

    public QuestObjective GetQuestObjective(uint questObjectiveId)
    {
        return _questObjectives.LookupByKey(questObjectiveId);
    }

    public Quest GetQuestTemplate(uint questId)
    {
        return QuestTemplates.LookupByKey(questId);
    }

    public void Load()
    {
        var oldMSTime = Time.MSTime;

        // For reload case
        QuestTemplates.Clear();
        QuestTemplatesAutoPush.Clear();
        _questObjectives.Clear();
        _exclusiveQuestGroups.Clear();

        var result = _worldDatabase.Query("SELECT " +
                                          //0  1          2               3                4            5            6                  7                8                   9
                                          "ID, QuestType, QuestPackageID, ContentTuningID, QuestSortID, QuestInfoID, SuggestedGroupNum, RewardNextQuest, RewardXPDifficulty, RewardXPMultiplier, " +
                                          //10                    11                     12                13           14           15               16
                                          "RewardMoneyDifficulty, RewardMoneyMultiplier, RewardBonusMoney, RewardSpell, RewardHonor, RewardKillHonor, StartItem, " +
                                          //17                         18                          19                        20     21       22
                                          "RewardArtifactXPDifficulty, RewardArtifactXPMultiplier, RewardArtifactCategoryID, Flags, FlagsEx, FlagsEx2, " +
                                          //23          24             25         26                 27           28             29         30
                                          "RewardItem1, RewardAmount1, ItemDrop1, ItemDropQuantity1, RewardItem2, RewardAmount2, ItemDrop2, ItemDropQuantity2, " +
                                          //31          32             33         34                 35           36             37         38
                                          "RewardItem3, RewardAmount3, ItemDrop3, ItemDropQuantity3, RewardItem4, RewardAmount4, ItemDrop4, ItemDropQuantity4, " +
                                          //39                  40                         41                          42                   43                         44
                                          "RewardChoiceItemID1, RewardChoiceItemQuantity1, RewardChoiceItemDisplayID1, RewardChoiceItemID2, RewardChoiceItemQuantity2, RewardChoiceItemDisplayID2, " +
                                          //45                  46                         47                          48                   49                         50
                                          "RewardChoiceItemID3, RewardChoiceItemQuantity3, RewardChoiceItemDisplayID3, RewardChoiceItemID4, RewardChoiceItemQuantity4, RewardChoiceItemDisplayID4, " +
                                          //51                  52                         53                          54                   55                         56
                                          "RewardChoiceItemID5, RewardChoiceItemQuantity5, RewardChoiceItemDisplayID5, RewardChoiceItemID6, RewardChoiceItemQuantity6, RewardChoiceItemDisplayID6, " +
                                          //57           58    59    60           61           62                 63                 64
                                          "POIContinent, POIx, POIy, POIPriority, RewardTitle, RewardArenaPoints, RewardSkillLineID, RewardNumSkillUps, " +
                                          //65            66                  67                         68
                                          "PortraitGiver, PortraitGiverMount, PortraitGiverModelSceneID, PortraitTurnIn, " +
                                          //69               70                   71                      72                   73                74                   75                      76
                                          "RewardFactionID1, RewardFactionValue1, RewardFactionOverride1, RewardFactionCapIn1, RewardFactionID2, RewardFactionValue2, RewardFactionOverride2, RewardFactionCapIn2, " +
                                          //77               78                   79                      80                   81                82                   83                      84
                                          "RewardFactionID3, RewardFactionValue3, RewardFactionOverride3, RewardFactionCapIn3, RewardFactionID4, RewardFactionValue4, RewardFactionOverride4, RewardFactionCapIn4, " +
                                          //85               86                   87                      88                   89
                                          "RewardFactionID5, RewardFactionValue5, RewardFactionOverride5, RewardFactionCapIn5, RewardFactionFlags, " +
                                          //90                91                  92                 93                  94                 95                  96                 97
                                          "RewardCurrencyID1, RewardCurrencyQty1, RewardCurrencyID2, RewardCurrencyQty2, RewardCurrencyID3, RewardCurrencyQty3, RewardCurrencyID4, RewardCurrencyQty4, " +
                                          //98                 99                  100          101          102             103               104        105                  106
                                          "AcceptedSoundKitID, CompleteSoundKitID, AreaGroupID, TimeAllowed, AllowableRaces, TreasurePickerID, Expansion, ManagedWorldStateID, QuestSessionBonus, " +
                                          //107      108             109               110              111                112                113                 114                 115
                                          "LogTitle, LogDescription, QuestDescription, AreaDescription, PortraitGiverText, PortraitGiverName, PortraitTurnInText, PortraitTurnInName, QuestCompletionLog " +
                                          " FROM quest_template");

        if (result.IsEmpty())
        {
            Log.Logger.Information("Loaded 0 quests definitions. DB table `quest_template` is empty.");

            return;
        }

        // create multimap previous quest for each existed quest
        // some quests can have many previous maps set by NextQuestId in previous quest
        // for example set of race quests can lead to single not race specific quest
        do
        {
            var newQuest = _classFactory.ResolveWithPositionalParameters<Quest>(result.GetFields());
            QuestTemplates[newQuest.Id] = newQuest;

            if (newQuest.IsAutoPush)
                QuestTemplatesAutoPush.Add(newQuest);
        } while (result.NextRow());

        // Load `quest_reward_choice_items`
        //                               0        1      2      3      4      5      6
        result = _worldDatabase.Query("SELECT QuestID, Type1, Type2, Type3, Type4, Type5, Type6 FROM quest_reward_choice_items");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest reward choice items. DB table `quest_reward_choice_items` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadRewardChoiceItems(result.GetFields());
                else
                    Log.Logger.Error($"Table `quest_reward_choice_items` has data for quest {questId} but such quest does not exist");
            } while (result.NextRow());

        // Load `quest_reward_display_spell`
        //                               0        1        2
        result = _worldDatabase.Query("SELECT QuestID, SpellID, PlayerConditionID FROM quest_reward_display_spell ORDER BY QuestID ASC, Idx ASC");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest reward display spells. DB table `quest_reward_display_spell` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadRewardDisplaySpell(result.GetFields());
                else
                    Log.Logger.Error($"Table `quest_reward_display_spell` has data for quest {questId} but such quest does not exist");
            } while (result.NextRow());

        // Load `quest_details`
        //                               0   1       2       3       4       5            6            7            8
        result = _worldDatabase.Query("SELECT ID, Emote1, Emote2, Emote3, Emote4, EmoteDelay1, EmoteDelay2, EmoteDelay3, EmoteDelay4 FROM quest_details");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest details. DB table `quest_details` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadQuestDetails(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_details` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_request_items`
        //                               0   1                2                  3                     4                       5
        result = _worldDatabase.Query("SELECT ID, EmoteOnComplete, EmoteOnIncomplete, EmoteOnCompleteDelay, EmoteOnIncompleteDelay, CompletionText FROM quest_request_items");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest request items. DB table `quest_request_items` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadQuestRequestItems(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_request_items` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_offer_reward`
        //                               0   1       2       3       4       5            6            7            8            9
        result = _worldDatabase.Query("SELECT ID, Emote1, Emote2, Emote3, Emote4, EmoteDelay1, EmoteDelay2, EmoteDelay3, EmoteDelay4, RewardText FROM quest_offer_reward");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest reward emotes. DB table `quest_offer_reward` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadQuestOfferReward(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_offer_reward` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_template_addon`
        //                               0   1         2                 3              4            5            6               7                     8                     9
        result = _worldDatabase.Query("SELECT ID, MaxLevel, AllowableClasses, SourceSpellID, PrevQuestID, NextQuestID, ExclusiveGroup, BreadcrumbForQuestId, RewardMailTemplateID, RewardMailDelay, " +
                                      //10               11                   12                     13                     14                   15                   16
                                      "RequiredSkillID, RequiredSkillPoints, RequiredMinRepFaction, RequiredMaxRepFaction, RequiredMinRepValue, RequiredMaxRepValue, ProvidedItemCount, " +
                                      //17           18
                                      "SpecialFlags, ScriptName FROM quest_template_addon LEFT JOIN quest_mail_sender ON Id=QuestId");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest template addons. DB table `quest_template_addon` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadQuestTemplateAddon(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_template_addon` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_mail_sender`
        //                               0        1
        result = _worldDatabase.Query("SELECT QuestId, RewardMailSenderEntry FROM quest_mail_sender");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest mail senders. DB table `quest_mail_sender` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadQuestMailSender(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_mail_sender` has data for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_objectives`
        //                               0        1   2     3             4         5       6      7       8                  9
        result = _worldDatabase.Query("SELECT QuestID, ID, Type, StorageIndex, ObjectID, Amount, Flags, Flags2, ProgressBarWeight, Description FROM quest_objectives ORDER BY `Order` ASC, StorageIndex ASC");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest objectives. DB table `quest_objectives` is empty.");
        else
            do
            {
                var questId = result.Read<uint>(0);

                if (QuestTemplates.TryGetValue(questId, out var quest))
                    quest.LoadQuestObjective(result.GetFields());
                else
                    Log.Logger.Error("Table `quest_objectives` has objective for quest {0} but such quest does not exist", questId);
            } while (result.NextRow());

        // Load `quest_visual_effect` join table with quest_objectives because visual effects are based on objective ID (core stores objectives by their index in quest)
        //                                 0     1     2          3        4
        result = _worldDatabase.Query("SELECT v.ID, o.ID, o.QuestID, v.Index, v.VisualEffect FROM quest_visual_effect AS v LEFT JOIN quest_objectives AS o ON v.ID = o.ID ORDER BY v.Index DESC");

        if (result.IsEmpty())
            Log.Logger.Information("Loaded 0 quest visual effects. DB table `quest_visual_effect` is empty.");
        else
            do
            {
                var vID = result.Read<uint>(0);
                var oID = result.Read<uint>(1);

                if (vID == 0)
                {
                    Log.Logger.Error("Table `quest_visual_effect` has visual effect for null objective id");

                    continue;
                }

                // objID will be null if match for table join is not found
                if (vID != oID)
                {
                    Log.Logger.Error("Table `quest_visual_effect` has visual effect for objective {0} but such objective does not exist.", vID);

                    continue;
                }

                var questId = result.Read<uint>(2);

                // Do not throw error here because error for non existing quest is thrown while loading quest objectives. we do not need duplication
                var quest = QuestTemplates.LookupByKey(questId);

                quest?.LoadQuestObjectiveVisualEffect(result.GetFields());
            } while (result.NextRow());

        Dictionary<uint, uint> usedMailTemplates = new();

        // Post processing
        foreach (var qinfo in QuestTemplates.Values)
        {
            // skip post-loading checks for disabled quests
            if (_disableManager.IsDisabledFor(DisableType.Quest, qinfo.Id, null))
                continue;

            // additional quest integrity checks (GO, creaturetemplate and itemtemplate must be loaded already)

            if (qinfo.Type >= QuestType.Max)
                Log.Logger.Error("QuestId {0} has `Method` = {1}, expected values are 0, 1 or 2.", qinfo.Id, qinfo.Type);

            if (Convert.ToBoolean(qinfo.SpecialFlags & ~QuestSpecialFlags.DbAllowed))
            {
                Log.Logger.Error("QuestId {0} has `SpecialFlags` = {1} > max allowed value. Correct `SpecialFlags` to value <= {2}",
                                 qinfo.Id,
                                 qinfo.SpecialFlags,
                                 QuestSpecialFlags.DbAllowed);

                qinfo.SpecialFlags &= QuestSpecialFlags.DbAllowed;
            }

            if (qinfo.Flags.HasAnyFlag(QuestFlags.Daily) && qinfo.Flags.HasAnyFlag(QuestFlags.Weekly))
            {
                Log.Logger.Error("Weekly QuestId {0} is marked as daily quest in `Flags`, removed daily Id.", qinfo.Id);
                qinfo.Flags &= ~QuestFlags.Daily;
            }

            if (qinfo.Flags.HasAnyFlag(QuestFlags.Daily))
                if (!qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable))
                {
                    Log.Logger.Error("Daily QuestId {0} not marked as repeatable in `SpecialFlags`, added.", qinfo.Id);
                    qinfo.SpecialFlags |= QuestSpecialFlags.Repeatable;
                }

            if (qinfo.Flags.HasAnyFlag(QuestFlags.Weekly))
                if (!qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable))
                {
                    Log.Logger.Error("Weekly QuestId {0} not marked as repeatable in `SpecialFlags`, added.", qinfo.Id);
                    qinfo.SpecialFlags |= QuestSpecialFlags.Repeatable;
                }

            if (qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Monthly))
                if (!qinfo.SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable))
                {
                    Log.Logger.Error("Monthly quest {0} not marked as repeatable in `SpecialFlags`, added.", qinfo.Id);
                    qinfo.SpecialFlags |= QuestSpecialFlags.Repeatable;
                }

            if (Convert.ToBoolean(qinfo.Flags & QuestFlags.Tracking))
                // at auto-reward can be rewarded only RewardChoiceItemId[0]
                for (var j = 1; j < qinfo.RewardChoiceItemId.Length; ++j)
                {
                    var id = qinfo.RewardChoiceItemId[j];

                    if (id != 0)
                        Log.Logger.Error("QuestId {0} has `RewardChoiceItemId{1}` = {2} but item from `RewardChoiceItemId{3}` can't be rewarded with quest Id QUESTFLAGSTRACKING.",
                                         qinfo.Id,
                                         j + 1,
                                         id,
                                         j + 1);
                    // no changes, quest ignore this data
                }

            if (qinfo.ContentTuningId != 0 && !_contentTuningRecords.ContainsKey(qinfo.ContentTuningId))
                Log.Logger.Error($"QuestId {qinfo.Id} has `ContentTuningID` = {qinfo.ContentTuningId} but content tuning with this id does not exist.");

            // client quest log visual (area case)
            if (qinfo.QuestSortID > 0)
                if (!_areaTableRecords.ContainsKey(qinfo.QuestSortID))
                    Log.Logger.Error("QuestId {0} has `ZoneOrSort` = {1} (zone case) but zone with this id does not exist.",
                                     qinfo.Id,
                                     qinfo.QuestSortID);

            // no changes, quest not dependent from this value but can have problems at client
            // client quest log visual (sort case)
            if (qinfo.QuestSortID < 0)
            {
                if (!_questSortRecords.TryGetValue((uint)-qinfo.QuestSortID, out _))
                    Log.Logger.Error("QuestId {0} has `ZoneOrSort` = {1} (sort case) but quest sort with this id does not exist.",
                                     qinfo.Id,
                                     qinfo.QuestSortID);

                // no changes, quest not dependent from this value but can have problems at client (note some may be 0, we must allow this so no check)
                //check for proper RequiredSkillId value (skill case)
                var skillid = SharedConst.SkillByQuestSort(-qinfo.QuestSortID);

                if (skillid != SkillType.None)
                    if (qinfo.RequiredSkillId != (uint)skillid)
                        Log.Logger.Error("QuestId {0} has `ZoneOrSort` = {1} but `RequiredSkillId` does not have a corresponding value ({2}).",
                                         qinfo.Id,
                                         qinfo.QuestSortID,
                                         skillid);
                //override, and force proper value here?
            }

            // AllowableClasses, can be 0/CLASSMASK_ALL_PLAYABLE to allow any class
            if (qinfo.AllowableClasses != 0)
                if (!Convert.ToBoolean(qinfo.AllowableClasses & (uint)PlayerClass.ClassMaskAllPlayable))
                {
                    Log.Logger.Error("QuestId {0} does not contain any playable classes in `RequiredClasses` ({1}), value set to 0 (all classes).", qinfo.Id, qinfo.AllowableClasses);
                    qinfo.AllowableClasses = 0;
                }

            // AllowableRaces, can be -1/RACEMASK_ALL_PLAYABLE to allow any race
            if (qinfo.AllowableRaces != -1)
                if (qinfo.AllowableRaces > 0 && !Convert.ToBoolean(qinfo.AllowableRaces & (long)SharedConst.RaceMaskAllPlayable))
                {
                    Log.Logger.Error("QuestId {0} does not contain any playable races in `RequiredRaces` ({1}), value set to 0 (all races).", qinfo.Id, qinfo.AllowableRaces);
                    qinfo.AllowableRaces = -1;
                }

            // RequiredSkillId, can be 0
            if (qinfo.RequiredSkillId != 0)
                if (!_skillLineRecords.ContainsKey(qinfo.RequiredSkillId))
                    Log.Logger.Error("QuestId {0} has `RequiredSkillId` = {1} but this skill does not exist",
                                     qinfo.Id,
                                     qinfo.RequiredSkillId);

            if (qinfo.RequiredSkillPoints != 0)
                if (qinfo.RequiredSkillPoints > _worldManager.ConfigMaxSkillValue)
                    Log.Logger.Error("QuestId {0} has `RequiredSkillPoints` = {1} but max possible skill is {2}, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.RequiredSkillPoints,
                                     _worldManager.ConfigMaxSkillValue);
            // no changes, quest can't be done for this requirement
            // else Skill quests can have 0 skill level, this is ok

            if (qinfo.RequiredMinRepFaction != 0 && !_factionRecords.ContainsKey(qinfo.RequiredMinRepFaction))
                Log.Logger.Error("QuestId {0} has `RequiredMinRepFaction` = {1} but faction template {2} does not exist, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMinRepFaction,
                                 qinfo.RequiredMinRepFaction);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMaxRepFaction != 0 && !_factionRecords.ContainsKey(qinfo.RequiredMaxRepFaction))
                Log.Logger.Error("QuestId {0} has `RequiredMaxRepFaction` = {1} but faction template {2} does not exist, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMaxRepFaction,
                                 qinfo.RequiredMaxRepFaction);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMinRepValue != 0 && qinfo.RequiredMinRepValue > SharedConst.ReputationCap)
                Log.Logger.Error("QuestId {0} has `RequiredMinRepValue` = {1} but max reputation is {2}, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMinRepValue,
                                 SharedConst.ReputationCap);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMinRepValue != 0 && qinfo.RequiredMaxRepValue != 0 && qinfo.RequiredMaxRepValue <= qinfo.RequiredMinRepValue)
                Log.Logger.Error("QuestId {0} has `RequiredMaxRepValue` = {1} and `RequiredMinRepValue` = {2}, quest can't be done.",
                                 qinfo.Id,
                                 qinfo.RequiredMaxRepValue,
                                 qinfo.RequiredMinRepValue);

            // no changes, quest can't be done for this requirement
            if (qinfo.RequiredMinRepFaction == 0 && qinfo.RequiredMinRepValue != 0)
                Log.Logger.Error("QuestId {0} has `RequiredMinRepValue` = {1} but `RequiredMinRepFaction` is 0, value has no effect",
                                 qinfo.Id,
                                 qinfo.RequiredMinRepValue);

            // warning
            if (qinfo.RequiredMaxRepFaction == 0 && qinfo.RequiredMaxRepValue != 0)
                Log.Logger.Error("QuestId {0} has `RequiredMaxRepValue` = {1} but `RequiredMaxRepFaction` is 0, value has no effect",
                                 qinfo.Id,
                                 qinfo.RequiredMaxRepValue);

            // warning
            if (qinfo.RewardTitleId != 0 && !_charTitlesRecords.ContainsKey(qinfo.RewardTitleId))
            {
                Log.Logger.Error("QuestId {0} has `RewardTitleId` = {1} but CharTitle Id {1} does not exist, quest can't be rewarded with title.",
                                 qinfo.Id,
                                 qinfo.RewardTitleId);

                qinfo.RewardTitleId = 0;
                // quest can't reward this title
            }

            if (qinfo.SourceItemId != 0)
            {
                if (_itemTemplateCache.GetItemTemplate(qinfo.SourceItemId) == null)
                {
                    Log.Logger.Error("QuestId {0} has `SourceItemId` = {1} but item with entry {2} does not exist, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceItemId,
                                     qinfo.SourceItemId);

                    qinfo.SourceItemId = 0; // quest can't be done for this requirement
                }
                else if (qinfo.SourceItemIdCount == 0)
                {
                    if (_configuration.GetDefaultValue("load:autoclean", false))
                        _worldDatabase.Execute($"UPDATE quest_template_addon SET ProvidedItemCount = 1 WHERE ID = {qinfo.Id}");
                    else
                        Log.Logger.Error("QuestId {0} has `StartItem` = {1} but `ProvidedItemCount` = 0, set to 1 but need fix in DB.",
                                         qinfo.Id,
                                         qinfo.SourceItemId);

                    qinfo.SourceItemIdCount = 1; // update to 1 for allow quest work for backward compatibility with DB
                }
            }
            else if (qinfo.SourceItemIdCount > 0)
            {
                Log.Logger.Error("QuestId {0} has `SourceItemId` = 0 but `SourceItemIdCount` = {1}, useless value.",
                                 qinfo.Id,
                                 qinfo.SourceItemIdCount);

                qinfo.SourceItemIdCount = 0; // no quest work changes in fact
            }

            if (qinfo.SourceSpellID != 0)
            {
                var spellInfo = _spellManager.GetSpellInfo(qinfo.SourceSpellID);

                if (spellInfo == null)
                {
                    Log.Logger.Error("QuestId {0} has `SourceSpellid` = {1} but spell {1} doesn't exist, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceSpellID);

                    qinfo.SourceSpellID = 0; // quest can't be done for this requirement
                }
                else if (!_spellManager.IsSpellValid(spellInfo))
                {
                    Log.Logger.Error("QuestId {0} has `SourceSpellid` = {1} but spell {1} is broken, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.SourceSpellID);

                    qinfo.SourceSpellID = 0; // quest can't be done for this requirement
                }
            }

            foreach (var obj in qinfo.Objectives)
            {
                // Store objective for lookup by id
                _questObjectives[obj.Id] = obj;

                // Check storage index for objectives which store data
                if (obj.StorageIndex < 0)
                    switch (obj.Type)
                    {
                        case QuestObjectiveType.Monster:
                        case QuestObjectiveType.Item:
                        case QuestObjectiveType.GameObject:
                        case QuestObjectiveType.TalkTo:
                        case QuestObjectiveType.PlayerKills:
                        case QuestObjectiveType.AreaTrigger:
                        case QuestObjectiveType.WinPetBattleAgainstNpc:
                        case QuestObjectiveType.ObtainCurrency:
                            Log.Logger.Error("QuestId {0} objective {1} has invalid StorageIndex = {2} for objective type {3}", qinfo.Id, obj.Id, obj.StorageIndex, obj.Type);

                            break;
                    }

                switch (obj.Type)
                {
                    case QuestObjectiveType.Item:
                        if (_itemTemplateCache.GetItemTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing item entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.Monster:
                        if (_creatureTemplateCache.GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing creature entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.GameObject:
                        if (_gameObjectTemplateCache.GetGameObjectTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing gameobject entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.TalkTo:
                        if (_creatureTemplateCache.GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error($"QuestId {qinfo.Id} objective {obj.Id} has non existing creature entry {obj.ObjectID}, quest can't be done.");

                        break;

                    case QuestObjectiveType.MinReputation:
                    case QuestObjectiveType.MaxReputation:
                    case QuestObjectiveType.IncreaseReputation:
                        if (!_factionRecords.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing faction id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.PlayerKills:
                        if (obj.Amount <= 0)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has invalid player kills count {2}", qinfo.Id, obj.Id, obj.Amount);

                        break;

                    case QuestObjectiveType.Currency:
                    case QuestObjectiveType.HaveCurrency:
                    case QuestObjectiveType.ObtainCurrency:
                        if (!_currencyTypesRecords.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing currency {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        if (obj.Amount <= 0)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has invalid currency amount {2}", qinfo.Id, obj.Id, obj.Amount);

                        break;

                    case QuestObjectiveType.LearnSpell:
                        if (!_spellManager.HasSpellInfo((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing spell id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.WinPetBattleAgainstNpc:
                        if (obj.ObjectID != 0 && _creatureTemplateCache.GetCreatureTemplate((uint)obj.ObjectID) == null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing creature entry {2}, quest can't be done.", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.DefeatBattlePet:
                        if (!_battlePetSpeciesRecords.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing battlepet species id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.CriteriaTree:
                        if (!_criteriaTreeRecords.ContainsKey((uint)obj.ObjectID))
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing criteria tree id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.AreaTrigger:
                        if (!_areaTriggerRecords.ContainsKey((uint)obj.ObjectID) && obj.ObjectID != -1)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing AreaTrigger.db2 id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.AreaTriggerEnter:
                    case QuestObjectiveType.AreaTriggerExit:
                        if (_areaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)obj.ObjectID, false)) == null && _areaTriggerDataStorage.GetAreaTriggerTemplate(new AreaTriggerId((uint)obj.ObjectID, true)) != null)
                            if (_configuration.GetDefaultValue("load:autoclean", false))
                                _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                            else
                                Log.Logger.Error("QuestId {0} objective {1} has non existing areatrigger id {2}", qinfo.Id, obj.Id, obj.ObjectID);

                        break;

                    case QuestObjectiveType.Money:
                    case QuestObjectiveType.WinPvpPetBattles:
                    case QuestObjectiveType.ProgressBar:
                        break;

                    default:
                        if (_configuration.GetDefaultValue("load:autoclean", false))
                            _worldDatabase.Execute($"DELETE FROM quest_objectives WHERE QuestID = {obj.QuestID}");
                        else
                            Log.Logger.Error("QuestId {0} objective {1} has unhandled type {2}", qinfo.Id, obj.Id, obj.Type);

                        break;
                }

                if (obj.Flags.HasAnyFlag(QuestObjectiveFlags.Sequenced))
                    qinfo.SetSpecialFlag(QuestSpecialFlags.SequencedObjectives);
            }

            for (var j = 0; j < SharedConst.QuestItemDropCount; j++)
            {
                var id = qinfo.ItemDrop[j];

                if (id != 0)
                {
                    if (_itemTemplateCache.GetItemTemplate(id) == null)
                        Log.Logger.Error("QuestId {0} has `RequiredSourceItemId{1}` = {2} but item with entry {2} does not exist, quest can't be done.",
                                         qinfo.Id,
                                         j + 1,
                                         id);
                    // no changes, quest can't be done for this requirement
                }
                else
                {
                    if (qinfo.ItemDropQuantity[j] > 0)
                        Log.Logger.Error("QuestId {0} has `RequiredSourceItemId{1}` = 0 but `RequiredSourceItemCount{1}` = {2}.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.ItemDropQuantity[j]);
                    // no changes, quest ignore this data
                }
            }

            for (var j = 0; j < SharedConst.QuestRewardChoicesCount; ++j)
            {
                var id = qinfo.RewardChoiceItemId[j];

                if (id != 0)
                {
                    switch (qinfo.RewardChoiceItemType[j])
                    {
                        case LootItemType.Item:
                            if (_itemTemplateCache.GetItemTemplate(id) == null)
                            {
                                Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = {id} but item with entry {id} does not exist, quest will not reward this item.");
                                qinfo.RewardChoiceItemId[j] = 0; // no changes, quest will not reward this
                            }

                            break;

                        case LootItemType.Currency:
                            if (!_currencyTypesRecords.HasRecord(id))
                            {
                                Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = {id} but currency with id {id} does not exist, quest will not reward this currency.");
                                qinfo.RewardChoiceItemId[j] = 0; // no changes, quest will not reward this
                            }

                            break;

                        default:
                            Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemType{j + 1}` = {qinfo.RewardChoiceItemType[j]} but it is not a valid item type, reward removed.");
                            qinfo.RewardChoiceItemId[j] = 0;

                            break;
                    }

                    if (qinfo.RewardChoiceItemCount[j] == 0)
                        Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = {id} but `RewardChoiceItemCount{j + 1}` = 0, quest can't be done.");
                }
                else if (qinfo.RewardChoiceItemCount[j] > 0)
                    Log.Logger.Error($"QuestId {qinfo.Id} has `RewardChoiceItemId{j + 1}` = 0 but `RewardChoiceItemCount{j + 1}` = {qinfo.RewardChoiceItemCount[j]}.");
                // no changes, quest ignore this data
            }

            for (var j = 0; j < SharedConst.QuestRewardItemCount; ++j)
            {
                var id = qinfo.RewardItemId[j];

                if (id != 0)
                {
                    if (_itemTemplateCache.GetItemTemplate(id) == null)
                    {
                        Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = {2} but item with entry {3} does not exist, quest will not reward this item.",
                                         qinfo.Id,
                                         j + 1,
                                         id,
                                         id);

                        qinfo.RewardItemId[j] = 0; // no changes, quest will not reward this item
                    }

                    if (qinfo.RewardItemCount[j] == 0)
                        Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = {2} but `RewardItemIdCount{3}` = 0, quest will not reward this item.",
                                         qinfo.Id,
                                         j + 1,
                                         id,
                                         j + 1);
                    // no changes
                }
                else if (qinfo.RewardItemCount[j] > 0)
                    Log.Logger.Error("QuestId {0} has `RewardItemId{1}` = 0 but `RewardItemIdCount{2}` = {3}.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardItemCount[j]);
                // no changes, quest ignore this data
            }

            for (var j = 0; j < SharedConst.QuestRewardReputationsCount; ++j)
                if (qinfo.RewardFactionId[j] != 0)
                {
                    if (Math.Abs(qinfo.RewardFactionValue[j]) > 9)
                        Log.Logger.Error("QuestId {0} has RewardFactionValueId{1} = {2}. That is outside the range of valid values (-9 to 9).", qinfo.Id, j + 1, qinfo.RewardFactionValue[j]);

                    if (!_factionRecords.ContainsKey(qinfo.RewardFactionId[j]))
                    {
                        Log.Logger.Error("QuestId {0} has `RewardFactionId{1}` = {2} but raw faction (faction.dbc) {3} does not exist, quest will not reward reputation for this faction.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.RewardFactionId[j],
                                         qinfo.RewardFactionId[j]);

                        qinfo.RewardFactionId[j] = 0; // quest will not reward this
                    }
                }
                else if (qinfo.RewardFactionOverride[j] != 0)
                    Log.Logger.Error("QuestId {0} has `RewardFactionId{1}` = 0 but `RewardFactionValueIdOverride{2}` = {3}.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardFactionOverride[j]);

            // no changes, quest ignore this data
            if (qinfo.RewardSpell > 0)
            {
                var spellInfo = _spellManager.GetSpellInfo(qinfo.RewardSpell);

                if (spellInfo == null)
                {
                    Log.Logger.Error("QuestId {0} has `RewardSpellCast` = {1} but spell {2} does not exist, quest will not have a spell reward.",
                                     qinfo.Id,
                                     qinfo.RewardSpell,
                                     qinfo.RewardSpell);

                    qinfo.RewardSpell = 0; // no spell will be casted on player
                }
                else if (!_spellManager.IsSpellValid(spellInfo))
                {
                    Log.Logger.Error("QuestId {0} has `RewardSpellCast` = {1} but spell {2} is broken, quest will not have a spell reward.",
                                     qinfo.Id,
                                     qinfo.RewardSpell,
                                     qinfo.RewardSpell);

                    qinfo.RewardSpell = 0; // no spell will be casted on player
                }
            }

            if (qinfo.RewardMailTemplateId != 0)
            {
                if (!_mailTemplateRecords.ContainsKey(qinfo.RewardMailTemplateId))
                {
                    Log.Logger.Error("QuestId {0} has `RewardMailTemplateId` = {1} but mail template {2} does not exist, quest will not have a mail reward.",
                                     qinfo.Id,
                                     qinfo.RewardMailTemplateId,
                                     qinfo.RewardMailTemplateId);

                    qinfo.RewardMailTemplateId = 0; // no mail will send to player
                    qinfo.RewardMailDelay = 0;      // no mail will send to player
                    qinfo.RewardMailSenderEntry = 0;
                }
                else if (usedMailTemplates.ContainsKey(qinfo.RewardMailTemplateId))
                {
                    var usedId = usedMailTemplates.LookupByKey(qinfo.RewardMailTemplateId);

                    Log.Logger.Error("QuestId {0} has `RewardMailTemplateId` = {1} but mail template  {2} already used for quest {3}, quest will not have a mail reward.",
                                     qinfo.Id,
                                     qinfo.RewardMailTemplateId,
                                     qinfo.RewardMailTemplateId,
                                     usedId);

                    qinfo.RewardMailTemplateId = 0; // no mail will send to player
                    qinfo.RewardMailDelay = 0;      // no mail will send to player
                    qinfo.RewardMailSenderEntry = 0;
                }
                else
                    usedMailTemplates[qinfo.RewardMailTemplateId] = qinfo.Id;
            }

            if (qinfo.NextQuestInChain != 0)
                if (!QuestTemplates.ContainsKey(qinfo.NextQuestInChain))
                {
                    Log.Logger.Error("QuestId {0} has `NextQuestIdChain` = {1} but quest {2} does not exist, quest chain will not work.",
                                     qinfo.Id,
                                     qinfo.NextQuestInChain,
                                     qinfo.NextQuestInChain);

                    qinfo.NextQuestInChain = 0;
                }

            for (var j = 0; j < SharedConst.QuestRewardCurrencyCount; ++j)
                if (qinfo.RewardCurrencyId[j] != 0)
                {
                    if (qinfo.RewardCurrencyCount[j] == 0)
                        Log.Logger.Error("QuestId {0} has `RewardCurrencyId{1}` = {2} but `RewardCurrencyCount{3}` = 0, quest can't be done.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.RewardCurrencyId[j],
                                         j + 1);

                    // no changes, quest can't be done for this requirement
                    if (!_currencyTypesRecords.ContainsKey(qinfo.RewardCurrencyId[j]))
                    {
                        Log.Logger.Error("QuestId {0} has `RewardCurrencyId{1}` = {2} but currency with entry {3} does not exist, quest can't be done.",
                                         qinfo.Id,
                                         j + 1,
                                         qinfo.RewardCurrencyId[j],
                                         qinfo.RewardCurrencyId[j]);

                        qinfo.RewardCurrencyCount[j] = 0; // prevent incorrect work of quest
                    }
                }
                else if (qinfo.RewardCurrencyCount[j] > 0)
                {
                    Log.Logger.Error("QuestId {0} has `RewardCurrencyId{1}` = 0 but `RewardCurrencyCount{2}` = {3}, quest can't be done.",
                                     qinfo.Id,
                                     j + 1,
                                     j + 1,
                                     qinfo.RewardCurrencyCount[j]);

                    qinfo.RewardCurrencyCount[j] = 0; // prevent incorrect work of quest
                }

            if (qinfo.SoundAccept != 0)
                if (!_soundKitRecords.ContainsKey(qinfo.SoundAccept))
                {
                    Log.Logger.Error("QuestId {0} has `SoundAccept` = {1} but sound {2} does not exist, set to 0.",
                                     qinfo.Id,
                                     qinfo.SoundAccept,
                                     qinfo.SoundAccept);

                    qinfo.SoundAccept = 0; // no sound will be played
                }

            if (qinfo.SoundTurnIn != 0)
                if (!_soundKitRecords.ContainsKey(qinfo.SoundTurnIn))
                {
                    Log.Logger.Error("QuestId {0} has `SoundTurnIn` = {1} but sound {2} does not exist, set to 0.",
                                     qinfo.Id,
                                     qinfo.SoundTurnIn,
                                     qinfo.SoundTurnIn);

                    qinfo.SoundTurnIn = 0; // no sound will be played
                }

            if (qinfo.RewardSkillId > 0)
            {
                if (!_skillLineRecords.ContainsKey(qinfo.RewardSkillId))
                    Log.Logger.Error("QuestId {0} has `RewardSkillId` = {1} but this skill does not exist",
                                     qinfo.Id,
                                     qinfo.RewardSkillId);

                if (qinfo.RewardSkillPoints == 0)
                    Log.Logger.Error("QuestId {0} has `RewardSkillId` = {1} but `RewardSkillPoints` is 0",
                                     qinfo.Id,
                                     qinfo.RewardSkillId);
            }

            if (qinfo.RewardSkillPoints != 0)
            {
                if (qinfo.RewardSkillPoints > _worldManager.ConfigMaxSkillValue)
                    Log.Logger.Error("QuestId {0} has `RewardSkillPoints` = {1} but max possible skill is {2}, quest can't be done.",
                                     qinfo.Id,
                                     qinfo.RewardSkillPoints,
                                     _worldManager.ConfigMaxSkillValue);

                // no changes, quest can't be done for this requirement
                if (qinfo.RewardSkillId == 0)
                    Log.Logger.Error("QuestId {0} has `RewardSkillPoints` = {1} but `RewardSkillId` is 0",
                                     qinfo.Id,
                                     qinfo.RewardSkillPoints);
            }

            // fill additional data stores
            var prevQuestId = (uint)Math.Abs(qinfo.PrevQuestId);

            if (prevQuestId != 0)
            {
                if (!QuestTemplates.TryGetValue(prevQuestId, out var prevQuestItr))
                    Log.Logger.Error($"QuestId {qinfo.Id} has PrevQuestId {prevQuestId}, but no such quest");
                else if (prevQuestItr.BreadcrumbForQuestId != 0)
                    Log.Logger.Error($"QuestId {qinfo.Id} should not be unlocked by breadcrumb quest {prevQuestId}");
                else if (qinfo.PrevQuestId > 0)
                    qinfo.DependentPreviousQuests.Add(prevQuestId);
            }

            if (qinfo.NextQuestId != 0)
            {
                if (!QuestTemplates.TryGetValue(qinfo.NextQuestId, out var nextquest))
                    Log.Logger.Error("QuestId {0} has NextQuestId {1}, but no such quest", qinfo.Id, qinfo.NextQuestId);
                else
                    nextquest.DependentPreviousQuests.Add(qinfo.Id);
            }

            var breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);

            if (breadcrumbForQuestId != 0)
            {
                if (!QuestTemplates.ContainsKey(breadcrumbForQuestId))
                {
                    Log.Logger.Error($"QuestId {qinfo.Id} is a breadcrumb for quest {breadcrumbForQuestId}, but no such quest exists");
                    qinfo.BreadcrumbForQuestId = 0;
                }

                if (qinfo.NextQuestId != 0)
                    Log.Logger.Error($"QuestId {qinfo.Id} is a breadcrumb, should not unlock quest {qinfo.NextQuestId}");
            }

            if (qinfo.ExclusiveGroup != 0)
                _exclusiveQuestGroups.Add(qinfo.ExclusiveGroup, qinfo.Id);
        }

        foreach (var questPair in QuestTemplates)
        {
            // skip post-loading checks for disabled quests
            if (_disableManager.IsDisabledFor(DisableType.Quest, questPair.Key, null))
                continue;

            var qinfo = questPair.Value;
            var qid = qinfo.Id;
            var breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);
            List<uint> questSet = new();

            while (breadcrumbForQuestId != 0)
            {
                //a previously visited quest was found as a breadcrumb quest
                //breadcrumb loop found!
                if (questSet.Contains(qinfo.Id))
                {
                    Log.Logger.Error($"Breadcrumb quests {qid} and {breadcrumbForQuestId} are in a loop");
                    qinfo.BreadcrumbForQuestId = 0;

                    break;
                }

                questSet.Add(qinfo.Id);

                qinfo = GetQuestTemplate(breadcrumbForQuestId);

                //every quest has a list of every breadcrumb towards it
                qinfo.DependentBreadcrumbQuests.Add(qid);

                breadcrumbForQuestId = (uint)Math.Abs(qinfo.BreadcrumbForQuestId);
            }
        }

        // check QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT for spell with SPELL_EFFECT_QUEST_COMPLETE
        foreach (var spellNameEntry in _spellNameRecords.Values)
        {
            var spellInfo = _spellManager.GetSpellInfo(spellNameEntry.Id);

            if (spellInfo == null)
                continue;

            foreach (var spellEffectInfo in spellInfo.Effects)
            {
                if (spellEffectInfo.Effect != SpellEffectName.QuestComplete)
                    continue;

                var questId = (uint)spellEffectInfo.MiscValue;
                var quest = GetQuestTemplate(questId);

                // some quest referenced in spells not exist (outdated spells)
                if (quest == null)
                    continue;

                if (!quest.HasSpecialFlag(QuestSpecialFlags.ExplorationOrEvent))
                {
                    Log.Logger.Error("Spell (id: {0}) have SPELL_EFFECT_QUEST_COMPLETE for quest {1}, but quest not have Id QUEST_SPECIAL_FLAGS_EXPLORATION_OR_EVENT. " +
                                     "QuestId flags must be fixed, quest modified to enable objective.",
                                     spellInfo.Id,
                                     questId);

                    // this will prevent quest completing without objective
                    quest.SetSpecialFlag(QuestSpecialFlags.ExplorationOrEvent);
                }
            }
        }

        // Make all paragon reward quests repeatable
        foreach (var paragonReputation in _paragonReputationRecords.Values)
        {
            var quest = GetQuestTemplate((uint)paragonReputation.QuestID);

            quest?.SetSpecialFlag(QuestSpecialFlags.Repeatable);
        }

        Log.Logger.Information("Loaded {0} quests definitions in {1} ms", QuestTemplates.Count, Time.GetMSTimeDiffToNow(oldMSTime));
    }

    public bool TryGetQuestTemplate(uint questId, out Quest quest)
    {
        return QuestTemplates.TryGetValue(questId, out quest);
    }
}