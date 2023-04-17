﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Conditions;
using Forged.MapServer.DataStorage;
using Forged.MapServer.Entities.Players;
using Forged.MapServer.Globals;
using Forged.MapServer.Networking.Packets.Item;
using Forged.MapServer.Networking.Packets.Quest;
using Forged.MapServer.Pools;
using Forged.MapServer.Spells;
using Framework.Constants;
using Framework.Database;
using Framework.Util;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace Forged.MapServer.Quest;

public class Quest
{
    private readonly CliDB _cliDB;
    private readonly ConditionManager _conditionManager;
    private readonly IConfiguration _configuration;
    private readonly DB2Manager _db2Manager;
    private readonly GameObjectManager _gameObjectManager;
    private readonly QuestPoolManager _questPoolManager;
    private readonly SpellManager _spellManager;

    public Quest(SQLFields fields, IConfiguration configuration, DB2Manager db2Manager, CliDB cliDB, QuestPoolManager questPoolManager, GameObjectManager gameObjectManager,
                 SpellManager spellManager, ConditionManager conditionManager)
    {
        _configuration = configuration;
        _db2Manager = db2Manager;
        _cliDB = cliDB;
        _questPoolManager = questPoolManager;
        _gameObjectManager = gameObjectManager;
        _spellManager = spellManager;
        _conditionManager = conditionManager;
        Id = fields.Read<uint>(0);
        Type = (QuestType)fields.Read<byte>(1);
        PackageID = fields.Read<uint>(2);
        ContentTuningId = fields.Read<uint>(3);
        QuestSortID = fields.Read<short>(4);
        QuestInfoID = fields.Read<ushort>(5);
        SuggestedPlayers = fields.Read<uint>(6);
        NextQuestInChain = fields.Read<uint>(7);
        RewardXPDifficulty = fields.Read<uint>(8);
        RewardXPMultiplier = fields.Read<float>(9);
        RewardMoneyDifficulty = fields.Read<uint>(10);
        RewardMoneyMultiplier = fields.Read<float>(11);
        RewardBonusMoney = fields.Read<uint>(12);
        RewardSpell = fields.Read<uint>(13);
        RewardHonor = fields.Read<uint>(14);
        RewardKillHonor = fields.Read<uint>(15);
        SourceItemId = fields.Read<uint>(16);
        RewardArtifactXPDifficulty = fields.Read<uint>(17);
        RewardArtifactXPMultiplier = fields.Read<float>(18);
        RewardArtifactCategoryID = fields.Read<uint>(19);
        Flags = (QuestFlags)fields.Read<uint>(20);
        FlagsEx = (QuestFlagsEx)fields.Read<uint>(21);
        FlagsEx2 = (QuestFlagsEx2)fields.Read<uint>(22);

        for (var i = 0; i < SharedConst.QuestItemDropCount; ++i)
        {
            RewardItemId[i] = fields.Read<uint>(23 + i * 4);
            RewardItemCount[i] = fields.Read<uint>(24 + i * 4);
            ItemDrop[i] = fields.Read<uint>(25 + i * 4);
            ItemDropQuantity[i] = fields.Read<uint>(26 + i * 4);

            if (RewardItemId[i] != 0)
                ++RewItemsCount;
        }

        for (var i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
        {
            RewardChoiceItemId[i] = fields.Read<uint>(39 + i * 3);
            RewardChoiceItemCount[i] = fields.Read<uint>(40 + i * 3);
            RewardChoiceItemDisplayId[i] = fields.Read<uint>(41 + i * 3);

            if (RewardChoiceItemId[i] != 0)
                ++RewChoiceItemsCount;
        }

        POIContinent = fields.Read<uint>(57);
        PoIx = fields.Read<float>(58);
        PoIy = fields.Read<float>(59);
        POIPriority = fields.Read<uint>(60);

        RewardTitleId = fields.Read<uint>(61);
        RewardArenaPoints = fields.Read<int>(62);
        RewardSkillId = fields.Read<uint>(63);
        RewardSkillPoints = fields.Read<uint>(64);

        QuestGiverPortrait = fields.Read<uint>(65);
        QuestGiverPortraitMount = fields.Read<uint>(66);
        QuestGiverPortraitModelSceneId = fields.Read<int>(67);
        QuestTurnInPortrait = fields.Read<uint>(68);

        for (var i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
        {
            RewardFactionId[i] = fields.Read<uint>(69 + i * 4);
            RewardFactionValue[i] = fields.Read<int>(70 + i * 4);
            RewardFactionOverride[i] = fields.Read<int>(71 + i * 4);
            RewardFactionCapIn[i] = fields.Read<int>(72 + i * 4);
        }

        RewardReputationMask = fields.Read<uint>(89);

        for (var i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
        {
            RewardCurrencyId[i] = fields.Read<uint>(90 + i * 2);
            RewardCurrencyCount[i] = fields.Read<uint>(91 + i * 2);

            if (RewardCurrencyId[i] != 0)
                ++RewCurrencyCount;
        }

        SoundAccept = fields.Read<uint>(98);
        SoundTurnIn = fields.Read<uint>(99);
        AreaGroupID = fields.Read<uint>(100);
        LimitTime = fields.Read<uint>(101);
        AllowableRaces = (long)fields.Read<ulong>(102);
        TreasurePickerID = fields.Read<int>(103);
        Expansion = fields.Read<int>(104);
        ManagedWorldStateID = fields.Read<int>(105);
        QuestSessionBonus = fields.Read<int>(106);

        LogTitle = fields.Read<string>(107);
        LogDescription = fields.Read<string>(108);
        QuestDescription = fields.Read<string>(109);
        AreaDescription = fields.Read<string>(110);
        PortraitGiverText = fields.Read<string>(111);
        PortraitGiverName = fields.Read<string>(112);
        PortraitTurnInText = fields.Read<string>(113);
        PortraitTurnInName = fields.Read<string>(114);
        QuestCompletionLog = fields.Read<string>(115);
    }

    public uint AllowableClasses { get; set; }
    public long AllowableRaces { get; set; }
    public string AreaDescription { get; set; }
    public uint AreaGroupID { get; set; }
    public int BreadcrumbForQuestId { get; set; }
    public List<QuestConditionalText> ConditionalOfferRewardText { get; set; } = new();
    public List<QuestConditionalText> ConditionalQuestCompletionLog { get; set; } = new();
    public List<QuestConditionalText> ConditionalQuestDescription { get; set; } = new();
    public List<QuestConditionalText> ConditionalRequestItemsText { get; set; } = new();
    public uint ContentTuningId { get; set; }
    public List<uint> DependentBreadcrumbQuests { get; set; } = new();
    public List<uint> DependentPreviousQuests { get; set; } = new();
    public uint[] DetailsEmote { get; set; } = new uint[SharedConst.QuestEmoteCount];
    public uint[] DetailsEmoteDelay { get; set; } = new uint[SharedConst.QuestEmoteCount];
    public uint EmoteOnComplete { get; set; }
    public uint EmoteOnCompleteDelay { get; set; }
    public uint EmoteOnIncomplete { get; set; }
    public uint EmoteOnIncompleteDelay { get; set; }
    public ushort EventIdForQuest { get; set; }
    public int ExclusiveGroup { get; set; }
    public int Expansion { get; set; }
    public QuestFlags Flags { get; set; }
    public QuestFlagsEx FlagsEx { get; set; }
    public QuestFlagsEx2 FlagsEx2 { get; set; }
    public uint Id { get; set; }
    public bool IsAutoAccept => !_configuration.GetDefaultValue("Quests:IgnoreAutoAccept", false) && HasFlag(QuestFlags.AutoAccept);
    public bool IsAutoComplete => !_configuration.GetDefaultValue("Quests:IgnoreAutoComplete", false) && Type == QuestType.AutoComplete;
    public bool IsAutoPush => HasFlagEx(QuestFlagsEx.AutoPush);
    public bool IsDaily => Flags.HasAnyFlag(QuestFlags.Daily);
    public bool IsDailyOrWeekly => Flags.HasAnyFlag(QuestFlags.Daily | QuestFlags.Weekly);
    public bool IsDfQuest => SpecialFlags.HasAnyFlag(QuestSpecialFlags.DfQuest);

    public bool IsMonthly => SpecialFlags.HasAnyFlag(QuestSpecialFlags.Monthly);

    // table data accessors:
    public bool IsRepeatable => SpecialFlags.HasAnyFlag(QuestSpecialFlags.Repeatable);

    public bool IsSeasonal => QuestSortID is -(int)QuestSort.Seasonal or -(int)QuestSort.Special or -(int)QuestSort.LunarFestival or -(int)QuestSort.Midsummer or -(int)QuestSort.Brewfest or -(int)QuestSort.LoveIsInTheAir or -(int)QuestSort.Noblegarden && !IsRepeatable;

    // Possibly deprecated Id
    public bool IsUnavailable => HasFlag(QuestFlags.Unavailable);

    public bool IsWeekly => Flags.HasAnyFlag(QuestFlags.Weekly);
    public bool IsWorldQuest => HasFlagEx(QuestFlagsEx.IsWorldQuest);
    public uint[] ItemDrop { get; set; } = new uint[SharedConst.QuestItemDropCount];
    public uint[] ItemDropQuantity { get; set; } = new uint[SharedConst.QuestItemDropCount];
    public uint LimitTime { get; set; }
    public string LogDescription { get; set; }
    public string LogTitle { get; set; }

    public int ManagedWorldStateID { get; set; }

    // quest_template_addon table (custom data)
    public uint MaxLevel { get; set; }

    public uint MaxMoneyReward => MaxMoneyValue * _configuration.GetDefaultValue("Rate:QuestId:Money:Reward", 1u);

    public uint MaxMoneyValue
    {
        get
        {
            uint value = 0;
            var questLevels = _db2Manager.GetContentTuningData(ContentTuningId, 0);

            if (!questLevels.HasValue)
                return value;

            if (_cliDB.QuestMoneyRewardStorage.TryGetValue((uint)questLevels.Value.MaxLevel, out var money))
                value = (uint)(money.Difficulty[RewardMoneyDifficulty] * RewardMoneyMultiplier);

            return value;
        }
    }

    public uint NextQuestId { get; set; }
    public uint NextQuestInChain { get; set; }
    public List<QuestObjective> Objectives { get; set; } = new();
    public int[] OfferRewardEmote { get; set; } = new int[SharedConst.QuestEmoteCount];
    public uint[] OfferRewardEmoteDelay { get; set; } = new uint[SharedConst.QuestEmoteCount];
    public string OfferRewardText { get; set; } = "";
    public uint PackageID { get; set; }
    public uint POIContinent { get; set; }
    public uint POIPriority { get; set; }
    public float PoIx { get; set; }
    public float PoIy { get; set; }
    public string PortraitGiverName { get; set; }
    public string PortraitGiverText { get; set; }
    public string PortraitTurnInName { get; set; }
    public string PortraitTurnInText { get; set; }
    public int PrevQuestId { get; set; }
    public string QuestCompletionLog { get; set; }
    public string QuestDescription { get; set; }
    public uint QuestGiverPortrait { get; set; }
    public int QuestGiverPortraitModelSceneId { get; set; }
    public uint QuestGiverPortraitMount { get; set; }
    public uint QuestInfoID { get; set; }
    public int QuestSessionBonus { get; set; }
    public int QuestSortID { get; set; }

    public QuestTagType? QuestTag
    {
        get
        {
            if (_cliDB.QuestInfoStorage.TryGetValue(QuestInfoID, out var questInfo))
                return (QuestTagType)questInfo.Type;

            return null;
        }
    }

    public uint QuestTurnInPortrait { get; set; }
    public string RequestItemsText { get; set; } = "";
    public uint RequiredMaxRepFaction { get; set; }
    public int RequiredMaxRepValue { get; set; }
    public uint RequiredMinRepFaction { get; set; }
    public int RequiredMinRepValue { get; set; }
    public uint RequiredSkillId { get; set; }
    public uint RequiredSkillPoints { get; set; }
    public QueryQuestInfoResponse[] Response { get; set; } = new QueryQuestInfoResponse[(int)Locale.Total];
    public int RewardArenaPoints { get; set; }
    public uint RewardArtifactCategoryID { get; set; }
    public uint RewardArtifactXPDifficulty { get; set; }
    public float RewardArtifactXPMultiplier { get; set; }
    public uint RewardBonusMoney { get; set; }
    public uint[] RewardChoiceItemCount { get; set; } = new uint[SharedConst.QuestRewardChoicesCount];
    public uint[] RewardChoiceItemDisplayId { get; set; } = new uint[SharedConst.QuestRewardChoicesCount];
    public uint[] RewardChoiceItemId { get; set; } = new uint[SharedConst.QuestRewardChoicesCount];
    public LootItemType[] RewardChoiceItemType { get; set; } = new LootItemType[SharedConst.QuestRewardChoicesCount];
    public uint[] RewardCurrencyCount { get; set; } = new uint[SharedConst.QuestRewardCurrencyCount];
    public uint[] RewardCurrencyId { get; set; } = new uint[SharedConst.QuestRewardCurrencyCount];
    public List<QuestRewardDisplaySpell> RewardDisplaySpell { get; set; } = new();
    public int[] RewardFactionCapIn { get; set; } = new int[SharedConst.QuestRewardReputationsCount];
    public uint[] RewardFactionId { get; set; } = new uint[SharedConst.QuestRewardReputationsCount];
    public int[] RewardFactionOverride { get; set; } = new int[SharedConst.QuestRewardReputationsCount];
    public int[] RewardFactionValue { get; set; } = new int[SharedConst.QuestRewardReputationsCount];
    public uint RewardHonor { get; set; }
    public uint[] RewardItemCount { get; set; } = new uint[SharedConst.QuestRewardItemCount];
    public uint[] RewardItemId { get; set; } = new uint[SharedConst.QuestRewardItemCount];
    public uint RewardKillHonor { get; set; }
    public uint RewardMailDelay { get; set; }
    public uint RewardMailSenderEntry { get; set; }
    public uint RewardMailTemplateId { get; set; }
    public uint RewardMoneyDifficulty { get; set; }
    public float RewardMoneyMultiplier { get; set; }
    public uint RewardReputationMask { get; set; }
    public uint RewardSkillId { get; set; }
    public uint RewardSkillPoints { get; set; }
    public uint RewardSpell { get; set; }
    public uint RewardTitleId { get; set; }
    public uint RewardXPDifficulty { get; set; }
    public float RewardXPMultiplier { get; set; }
    public uint RewChoiceItemsCount { get; }
    public uint RewCurrencyCount { get; }
    public uint RewItemsCount { get; }
    public uint ScriptId { get; set; }
    public uint SoundAccept { get; set; }
    public uint SoundTurnIn { get; set; }
    public uint SourceItemId { get; set; }
    public uint SourceItemIdCount { get; set; }
    public uint SourceSpellID { get; set; }
    public QuestSpecialFlags SpecialFlags { get; set; }
    public uint SuggestedPlayers { get; set; }
    public int TreasurePickerID { get; set; }
    public QuestType Type { get; set; }

    public BitArray UsedQuestObjectiveTypes { get; set; } = new((int)QuestObjectiveType.Max);

    // custom flags, not sniffed/WDB
    public static uint RoundXPValue(uint xp)
    {
        return xp switch
        {
            <= 100  => 5 * ((xp + 2) / 5),
            <= 500  => 10 * ((xp + 5) / 10),
            <= 1000 => 25 * ((xp + 12) / 25),
            _       => 50 * ((xp + 25) / 50)
        };
    }

    public static uint XPValue(Player player, uint contentTuningId, uint xpDifficulty, float xpMultiplier = 1.0f, int expansion = -1)
    {
        if (player == null)
            return 0;

        var questLevel = (uint)player.GetQuestLevel(contentTuningId);
        var questXp = player.CliDB.QuestXPStorage.LookupByKey(questLevel);

        if (questXp == null || xpDifficulty >= 10)
            return 0;

        var diffFactor = (int)(2 * (questLevel - player.Level) + 12);

        diffFactor = diffFactor switch
        {
            < 1  => 1,
            > 10 => 10,
            _    => diffFactor
        };

        var xp = (uint)(diffFactor * questXp.Difficulty[xpDifficulty] * xpMultiplier / 10);

        if (player.Level >= player.ObjectManager.GetMaxLevelForExpansion((Expansion)player.Configuration.GetDefaultValue("Expansion", (int)Framework.Constants.Expansion.Dragonflight) - 1) &&
            player.Session.Expansion == (Expansion)player.Configuration.GetDefaultValue("Expansion", (int)Framework.Constants.Expansion.Dragonflight) &&
            expansion >= 0 &&
            expansion < player.Configuration.GetDefaultValue("Expansion", (int)Framework.Constants.Expansion.Dragonflight))
            xp = (uint)(xp / 9.0f);

        xp = RoundXPValue(xp);

        if (player.Configuration.GetDefaultValue("MinQuestScaledXPRatio", 0) == 0)
            return xp;

        var minScaledXP = RoundXPValue((uint)(questXp.Difficulty[xpDifficulty] * xpMultiplier)) * player.Configuration.GetDefaultValue("MinQuestScaledXPRatio", 0u) / 100;
        xp = Math.Max(minScaledXP, xp);

        return xp;
    }

    public QueryQuestInfoResponse BuildQueryData(Locale loc, Player player)
    {
        QueryQuestInfoResponse response = new()
        {
            Allow = true,
            QuestID = Id,
            Info =
            {
                LogTitle = LogTitle,
                LogDescription = LogDescription,
                QuestDescription = QuestDescription,
                AreaDescription = AreaDescription,
                QuestCompletionLog = QuestCompletionLog,
                PortraitGiverText = PortraitGiverText,
                PortraitGiverName = PortraitGiverName,
                PortraitTurnInText = PortraitTurnInText,
                PortraitTurnInName = PortraitTurnInName,
                ConditionalQuestDescription = ConditionalQuestDescription.Select(text =>
                                                                         {
                                                                             var content = text.Text[(int)Locale.enUS];
                                                                             _gameObjectManager.GetLocaleString(text.Text, loc, ref content);

                                                                             return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
                                                                         })
                                                                         .ToList(),
                ConditionalQuestCompletionLog = ConditionalQuestCompletionLog.Select(text =>
                                                                             {
                                                                                 var content = text.Text[(int)Locale.enUS];
                                                                                 _gameObjectManager.GetLocaleString(text.Text, loc, ref content);

                                                                                 return new ConditionalQuestText(text.PlayerConditionId, text.QuestgiverCreatureId, content);
                                                                             })
                                                                             .ToList()
            }
        };

        if (loc != Locale.enUS)
        {
            var questTemplateLocale = _gameObjectManager.GetQuestLocale(Id);

            if (questTemplateLocale != null)
            {
                _gameObjectManager.GetLocaleString(questTemplateLocale.LogTitle, loc, ref response.Info.LogTitle);
                _gameObjectManager.GetLocaleString(questTemplateLocale.LogDescription, loc, ref response.Info.LogDescription);
                _gameObjectManager.GetLocaleString(questTemplateLocale.QuestDescription, loc, ref response.Info.QuestDescription);
                _gameObjectManager.GetLocaleString(questTemplateLocale.AreaDescription, loc, ref response.Info.AreaDescription);
                _gameObjectManager.GetLocaleString(questTemplateLocale.QuestCompletionLog, loc, ref response.Info.QuestCompletionLog);
                _gameObjectManager.GetLocaleString(questTemplateLocale.PortraitGiverText, loc, ref response.Info.PortraitGiverText);
                _gameObjectManager.GetLocaleString(questTemplateLocale.PortraitGiverName, loc, ref response.Info.PortraitGiverName);
                _gameObjectManager.GetLocaleString(questTemplateLocale.PortraitTurnInText, loc, ref response.Info.PortraitTurnInText);
                _gameObjectManager.GetLocaleString(questTemplateLocale.PortraitTurnInName, loc, ref response.Info.PortraitTurnInName);
            }
        }

        response.Info.QuestID = Id;
        response.Info.QuestType = (int)Type;
        response.Info.ContentTuningID = ContentTuningId;
        response.Info.QuestPackageID = PackageID;
        response.Info.QuestSortID = QuestSortID;
        response.Info.QuestInfoID = QuestInfoID;
        response.Info.SuggestedGroupNum = SuggestedPlayers;
        response.Info.RewardNextQuest = NextQuestInChain;
        response.Info.RewardXPDifficulty = RewardXPDifficulty;
        response.Info.RewardXPMultiplier = RewardXPMultiplier;

        if (!HasFlag(QuestFlags.HiddenRewards))
            response.Info.RewardMoney = (int)(player?.GetQuestMoneyReward(this) ?? MaxMoneyReward);

        response.Info.RewardMoneyDifficulty = RewardMoneyDifficulty;
        response.Info.RewardMoneyMultiplier = RewardMoneyMultiplier;
        response.Info.RewardBonusMoney = RewardBonusMoney;

        foreach (var displaySpell in RewardDisplaySpell)
            response.Info.RewardDisplaySpell.Add(new QuestCompleteDisplaySpell(displaySpell.SpellId, displaySpell.PlayerConditionId));

        response.Info.RewardSpell = RewardSpell;

        response.Info.RewardHonor = RewardHonor;
        response.Info.RewardKillHonor = RewardKillHonor;

        response.Info.RewardArtifactXPDifficulty = (int)RewardArtifactXPDifficulty;
        response.Info.RewardArtifactXPMultiplier = RewardArtifactXPMultiplier;
        response.Info.RewardArtifactCategoryID = (int)RewardArtifactCategoryID;

        response.Info.StartItem = SourceItemId;
        response.Info.Flags = (uint)Flags;
        response.Info.FlagsEx = (uint)FlagsEx;
        response.Info.FlagsEx2 = (uint)FlagsEx2;
        response.Info.RewardTitle = RewardTitleId;
        response.Info.RewardArenaPoints = RewardArenaPoints;
        response.Info.RewardSkillLineID = RewardSkillId;
        response.Info.RewardNumSkillUps = RewardSkillPoints;
        response.Info.RewardFactionFlags = RewardReputationMask;
        response.Info.PortraitGiver = QuestGiverPortrait;
        response.Info.PortraitGiverMount = QuestGiverPortraitMount;
        response.Info.PortraitGiverModelSceneID = QuestGiverPortraitModelSceneId;
        response.Info.PortraitTurnIn = QuestTurnInPortrait;

        for (byte i = 0; i < SharedConst.QuestItemDropCount; ++i)
        {
            response.Info.ItemDrop[i] = (int)ItemDrop[i];
            response.Info.ItemDropQuantity[i] = (int)ItemDropQuantity[i];
        }

        if (!HasFlag(QuestFlags.HiddenRewards))
        {
            for (byte i = 0; i < SharedConst.QuestRewardItemCount; ++i)
            {
                response.Info.RewardItems[i] = RewardItemId[i];
                response.Info.RewardAmount[i] = RewardItemCount[i];
            }

            for (byte i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            {
                response.Info.UnfilteredChoiceItems[i].ItemID = RewardChoiceItemId[i];
                response.Info.UnfilteredChoiceItems[i].Quantity = RewardChoiceItemCount[i];
            }
        }

        for (byte i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
        {
            response.Info.RewardFactionID[i] = RewardFactionId[i];
            response.Info.RewardFactionValue[i] = RewardFactionValue[i];
            response.Info.RewardFactionOverride[i] = RewardFactionOverride[i];
            response.Info.RewardFactionCapIn[i] = RewardFactionCapIn[i];
        }

        response.Info.POIContinent = POIContinent;
        response.Info.POIx = PoIx;
        response.Info.POIy = PoIy;
        response.Info.POIPriority = POIPriority;

        response.Info.AllowableRaces = AllowableRaces;
        response.Info.TreasurePickerID = TreasurePickerID;
        response.Info.Expansion = Expansion;
        response.Info.ManagedWorldStateID = ManagedWorldStateID;
        response.Info.QuestSessionBonus = 0;    //GetQuestSessionBonus(); // this is only sent while quest session is active
        response.Info.QuestGiverCreatureID = 0; // only sent during npc interaction

        foreach (var questObjective in Objectives)
        {
            response.Info.Objectives.Add(questObjective);

            if (loc == Locale.enUS)
                continue;

            var questObjectivesLocale = _gameObjectManager.GetQuestObjectivesLocale(questObjective.Id);

            if (questObjectivesLocale == null)
                continue;

            var desc = string.Empty;
            _gameObjectManager.GetLocaleString(questObjectivesLocale.Description, loc, ref desc);
            response.Info.Objectives.Last().Description = desc;
        }

        for (var i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
        {
            response.Info.RewardCurrencyID[i] = RewardCurrencyId[i];
            response.Info.RewardCurrencyQty[i] = RewardCurrencyCount[i];
        }

        response.Info.AcceptedSoundKitID = SoundAccept;
        response.Info.CompleteSoundKitID = SoundTurnIn;
        response.Info.AreaGroupID = AreaGroupID;
        response.Info.TimeAllowed = LimitTime;

        response.Write();

        return response;
    }

    public void BuildQuestRewards(QuestRewards rewards, Player player)
    {
        rewards.ChoiceItemCount = RewChoiceItemsCount;
        rewards.ItemCount = RewItemsCount;
        rewards.Money = player.GetQuestMoneyReward(this);
        rewards.XP = player.GetQuestXPReward(this);
        rewards.ArtifactCategoryID = RewardArtifactCategoryID;
        rewards.Title = RewardTitleId;
        rewards.FactionFlags = RewardReputationMask;

        var displaySpellIndex = 0;

        foreach (var displaySpell in RewardDisplaySpell)
        {
            if (_cliDB.PlayerConditionStorage.TryGetValue(displaySpell.PlayerConditionId, out var playerCondition))
                if (!_conditionManager.IsPlayerMeetingCondition(player, playerCondition))
                    continue;

            rewards.SpellCompletionDisplayID[displaySpellIndex] = (int)displaySpell.SpellId;

            if (++displaySpellIndex >= rewards.SpellCompletionDisplayID.Length)
                break;
        }

        rewards.SpellCompletionID = RewardSpell;
        rewards.SkillLineID = RewardSkillId;
        rewards.NumSkillUps = RewardSkillPoints;
        rewards.TreasurePickerID = (uint)TreasurePickerID;

        for (var i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
        {
            rewards.ChoiceItems[i].LootItemType = RewardChoiceItemType[i];
            rewards.ChoiceItems[i].Item = new ItemInstance();
            rewards.ChoiceItems[i].Item.ItemID = RewardChoiceItemId[i];
            rewards.ChoiceItems[i].Quantity = RewardChoiceItemCount[i];
        }

        for (var i = 0; i < SharedConst.QuestRewardItemCount; ++i)
        {
            rewards.ItemID[i] = RewardItemId[i];
            rewards.ItemQty[i] = RewardItemCount[i];
        }

        for (var i = 0; i < SharedConst.QuestRewardReputationsCount; ++i)
        {
            rewards.FactionID[i] = RewardFactionId[i];
            rewards.FactionOverride[i] = RewardFactionOverride[i];
            rewards.FactionValue[i] = RewardFactionValue[i];
            rewards.FactionCapIn[i] = RewardFactionCapIn[i];
        }

        for (var i = 0; i < SharedConst.QuestRewardCurrencyCount; ++i)
        {
            rewards.CurrencyID[i] = RewardCurrencyId[i];
            rewards.CurrencyQty[i] = RewardCurrencyCount[i];
        }
    }

    public uint CalculateHonorGain(uint level)
    {
        uint honor = 0;

        return honor;
    }

    public bool CanIncreaseRewardedQuestCounters()
    {
        // Dungeon Finder/Daily/Repeatable (if not weekly, monthly or seasonal) quests are never considered rewarded serverside.
        // This affects counters and client requests for completed quests.
        return !IsDfQuest && !IsDaily && (!IsRepeatable || IsWeekly || IsMonthly || IsSeasonal);
    }

    public uint GetRewMoneyMaxLevel()
    {
        // If QuestId has Id to not give money on max level, it's 0
        if (HasFlag(QuestFlags.NoMoneyFromXp))
            return 0;

        // Else, return the rewarded copper sum modified by the rate
        return RewardBonusMoney * _configuration.GetDefaultValue("Rate:QuestId:Money:Max:Level:Reward", 1u);
    }

    public bool HasFlag(QuestFlags flag)
    {
        return (Flags & flag) != 0;
    }

    public bool HasFlagEx(QuestFlagsEx flag)
    {
        return (FlagsEx & flag) != 0;
    }

    public bool HasFlagEx(QuestFlagsEx2 flag)
    {
        return (FlagsEx2 & flag) != 0;
    }

    public bool HasQuestObjectiveType(QuestObjectiveType type)
    {
        return UsedQuestObjectiveTypes[(int)type];
    }

    public bool HasSpecialFlag(QuestSpecialFlags flag)
    {
        return (SpecialFlags & flag) != 0;
    }

    public void InitializeQueryData()
    {
        for (var loc = Locale.enUS; loc < Locale.Total; ++loc)
            Response[(int)loc] = BuildQueryData(loc, null);
    }

    public bool IsAllowedInRaid(Difficulty difficulty)
    {
        return IsRaidQuest(difficulty) || _configuration.GetDefaultValue("Quests:IgnoreRaid", false);
    }

    public bool IsRaidQuest(Difficulty difficulty)
    {
        switch ((QuestInfos)QuestInfoID)
        {
            case QuestInfos.Raid:
                return true;
            case QuestInfos.Raid10:
                return difficulty is Difficulty.Raid10N or Difficulty.Raid10HC;
            case QuestInfos.Raid25:
                return difficulty is Difficulty.Raid25N or Difficulty.Raid25HC;
        }

        return Flags.HasAnyFlag(QuestFlags.Raid);
    }

    public void LoadQuestDetails(SQLFields fields)
    {
        for (var i = 0; i < SharedConst.QuestEmoteCount; ++i)
        {
            var emoteId = fields.Read<ushort>(1 + i);

            if (!_cliDB.EmotesStorage.ContainsKey(emoteId))
            {
                Log.Logger.Error("Table `quest_details` has non-existing Emote{0} ({1}) set for quest {2}. Skipped.", 1 + i, emoteId, fields.Read<uint>(0));

                continue;
            }

            DetailsEmote[i] = emoteId;
        }

        for (var i = 0; i < SharedConst.QuestEmoteCount; ++i)
            DetailsEmoteDelay[i] = fields.Read<uint>(5 + i);
    }

    public void LoadQuestMailSender(SQLFields fields)
    {
        RewardMailSenderEntry = fields.Read<uint>(1);
    }

    public void LoadQuestObjective(SQLFields fields)
    {
        QuestObjective obj = new()
        {
            QuestID = fields.Read<uint>(0),
            Id = fields.Read<uint>(1),
            Type = (QuestObjectiveType)fields.Read<byte>(2),
            StorageIndex = fields.Read<sbyte>(3),
            ObjectID = fields.Read<int>(4),
            Amount = fields.Read<int>(5),
            Flags = (QuestObjectiveFlags)fields.Read<uint>(6),
            Flags2 = fields.Read<uint>(7),
            ProgressBarWeight = fields.Read<float>(8),
            Description = fields.Read<string>(9)
        };

        Objectives.Add(obj);
        UsedQuestObjectiveTypes[(int)obj.Type] = true;
    }

    public void LoadQuestObjectiveVisualEffect(SQLFields fields)
    {
        var objID = fields.Read<uint>(1);

        foreach (var obj in Objectives)
            if (obj.Id == objID)
            {
                var effectIndex = fields.Read<byte>(3);

                obj.VisualEffects ??= new int[effectIndex + 1];

                if (effectIndex >= obj.VisualEffects.Length)
                {
                    var tmp = obj.VisualEffects;
                    Array.Resize(ref tmp, effectIndex + 1);
                    obj.VisualEffects = tmp;
                }

                obj.VisualEffects[effectIndex] = fields.Read<int>(4);

                break;
            }
    }

    public void LoadQuestOfferReward(SQLFields fields)
    {
        for (var i = 0; i < SharedConst.QuestEmoteCount; ++i)
        {
            var emoteId = fields.Read<short>(1 + i);

            if (emoteId < 0 || !_cliDB.EmotesStorage.ContainsKey(emoteId))
            {
                Log.Logger.Error("Table `quest_offer_reward` has non-existing Emote{0} ({1}) set for quest {2}. Skipped.", 1 + i, emoteId, fields.Read<uint>(0));

                continue;
            }

            OfferRewardEmote[i] = emoteId;
        }

        for (var i = 0; i < SharedConst.QuestEmoteCount; ++i)
            OfferRewardEmoteDelay[i] = fields.Read<uint>(5 + i);

        OfferRewardText = fields.Read<string>(9);
    }

    public void LoadQuestRequestItems(SQLFields fields)
    {
        EmoteOnComplete = fields.Read<ushort>(1);
        EmoteOnIncomplete = fields.Read<ushort>(2);

        if (!_cliDB.EmotesStorage.ContainsKey(EmoteOnComplete))
            Log.Logger.Error("Table `quest_request_items` has non-existing EmoteOnComplete ({0}) set for quest {1}.", EmoteOnComplete, fields.Read<uint>(0));

        if (!_cliDB.EmotesStorage.ContainsKey(EmoteOnIncomplete))
            Log.Logger.Error("Table `quest_request_items` has non-existing EmoteOnIncomplete ({0}) set for quest {1}.", EmoteOnIncomplete, fields.Read<uint>(0));

        EmoteOnCompleteDelay = fields.Read<uint>(3);
        EmoteOnIncompleteDelay = fields.Read<uint>(4);
        RequestItemsText = fields.Read<string>(5);
    }

    public void LoadQuestTemplateAddon(SQLFields fields)
    {
        MaxLevel = fields.Read<byte>(1);
        AllowableClasses = fields.Read<uint>(2);
        SourceSpellID = fields.Read<uint>(3);
        PrevQuestId = fields.Read<int>(4);
        NextQuestId = fields.Read<uint>(5);
        ExclusiveGroup = fields.Read<int>(6);
        BreadcrumbForQuestId = fields.Read<int>(7);
        RewardMailTemplateId = fields.Read<uint>(8);
        RewardMailDelay = fields.Read<uint>(9);
        RequiredSkillId = fields.Read<ushort>(10);
        RequiredSkillPoints = fields.Read<ushort>(11);
        RequiredMinRepFaction = fields.Read<ushort>(12);
        RequiredMaxRepFaction = fields.Read<ushort>(13);
        RequiredMinRepValue = fields.Read<int>(14);
        RequiredMaxRepValue = fields.Read<int>(15);
        SourceItemIdCount = fields.Read<byte>(16);
        SpecialFlags = (QuestSpecialFlags)fields.Read<byte>(17);
        ScriptId = _gameObjectManager.GetScriptId(fields.Read<string>(18));

        if (SpecialFlags.HasAnyFlag(QuestSpecialFlags.AutoAccept))
            Flags |= QuestFlags.AutoAccept;
    }

    public void LoadRewardChoiceItems(SQLFields fields)
    {
        for (var i = 0; i < SharedConst.QuestRewardChoicesCount; ++i)
            RewardChoiceItemType[i] = (LootItemType)fields.Read<byte>(1 + i);
    }

    public void LoadRewardDisplaySpell(SQLFields fields)
    {
        var spellId = fields.Read<uint>(1);
        var playerConditionId = fields.Read<uint>(2);

        if (!_spellManager.HasSpellInfo(spellId, Difficulty.None))
        {
            Log.Logger.Error($"Table `quest_reward_display_spell` has non-existing Spell ({spellId}) set for quest {Id}. Skipped.");

            return;
        }

        if (playerConditionId != 0 && !_cliDB.PlayerConditionStorage.ContainsKey(playerConditionId))
        {
            Log.Logger.Error($"Table `quest_reward_display_spell` has non-existing PlayerCondition ({playerConditionId}) set for quest {Id}. and spell {spellId} Set to 0.");
            playerConditionId = 0;
        }

        RewardDisplaySpell.Add(new QuestRewardDisplaySpell(spellId, playerConditionId));
    }

    public uint MoneyValue(Player player)
    {
        if (_cliDB.QuestMoneyRewardStorage.TryGetValue((uint)player.GetQuestLevel(this), out var money))
            return (uint)(money.Difficulty[RewardMoneyDifficulty] * RewardMoneyMultiplier);

        return 0;
    }

    public void SetSpecialFlag(QuestSpecialFlags flag)
    {
        SpecialFlags |= flag;
    }

    public uint XPValue(Player player)
    {
        return XPValue(player, ContentTuningId, RewardXPDifficulty, RewardXPMultiplier, Expansion);
    }
}