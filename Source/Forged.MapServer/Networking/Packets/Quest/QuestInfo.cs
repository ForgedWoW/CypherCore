// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Quest;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestInfo
{
    public uint AcceptedSoundKitID;
    public long AllowableRaces = -1;
    public string AreaDescription;
    public uint AreaGroupID;
    public uint CompleteSoundKitID;
    public List<ConditionalQuestText> ConditionalQuestCompletionLog = new();
    public List<ConditionalQuestText> ConditionalQuestDescription = new();
    public uint ContentTuningID;
    public int Expansion;
    public uint Flags;
    public uint FlagsEx;
    public uint FlagsEx2;
    public int[] ItemDrop = new int[SharedConst.QuestItemDropCount];
    public int[] ItemDropQuantity = new int[SharedConst.QuestItemDropCount];
    public string LogDescription;
    public string LogTitle;
    public int ManagedWorldStateID;
    public List<QuestObjective> Objectives = new();
    public uint POIContinent;
    public uint POIPriority;
    public float POIx;
    public float POIy;
    public uint PortraitGiver;

    public int PortraitGiverModelSceneID;

    // quest giver entry ?
    public uint PortraitGiverMount;

    public string PortraitGiverName;
    public string PortraitGiverText;
    public uint PortraitTurnIn;

    public string PortraitTurnInName;

    // quest turn in entry ?
    public string PortraitTurnInText;

    public string QuestCompletionLog;
    public string QuestDescription;
    public int QuestGiverCreatureID;
    public uint QuestID;
    public uint QuestInfoID;
    public uint QuestPackageID;
    public int QuestSessionBonus;
    public int QuestSortID;
    public int QuestType; // Accepted values: 0, 1 or 2. 0 == IsAutoComplete() (skip objectives/details)
    public bool ReadyForTranslation;

    public uint[] RewardAmount = new uint[SharedConst.QuestRewardItemCount];

    public int RewardArenaPoints;

    public int RewardArtifactCategoryID;

    public int RewardArtifactXPDifficulty;

    public float RewardArtifactXPMultiplier;

    public uint RewardBonusMoney;

    public uint[] RewardCurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];

    public uint[] RewardCurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];

    public List<QuestCompleteDisplaySpell> RewardDisplaySpell = new();

    public int[] RewardFactionCapIn = new int[SharedConst.QuestRewardReputationsCount];

    public uint RewardFactionFlags;

    public uint[] RewardFactionID = new uint[SharedConst.QuestRewardReputationsCount];

    public int[] RewardFactionOverride = new int[SharedConst.QuestRewardReputationsCount];

    public int[] RewardFactionValue = new int[SharedConst.QuestRewardReputationsCount];

    public uint RewardHonor;

    // used to select ConditionalQuestText
    public uint[] RewardItems = new uint[SharedConst.QuestRewardItemCount];

    public float RewardKillHonor;

    public int RewardMoney;

    // reward money (below max lvl)
    public uint RewardMoneyDifficulty;

    public float RewardMoneyMultiplier = 1.0f;

    public uint RewardNextQuest;

    public uint RewardNumSkillUps;

    public uint RewardSkillLineID;

    // reward spell, this spell will be displayed (icon)
    public uint RewardSpell;

    public uint RewardTitle;

    // client will request this quest from NPC, if not 0
    public uint RewardXPDifficulty;

    // used for calculating rewarded experience
    public float RewardXPMultiplier = 1.0f;

    public uint StartItem;

    // zone or sort to display in quest log
    public uint SuggestedGroupNum;

    // new 2.4.0, player gets this title (id from CharTitles)
    // reward skill id
    // reward skill points
    // rep mask (unsure on what it does)
    public long TimeAllowed;
    public int TreasurePickerID;
    public QuestInfoChoiceItem[] UnfilteredChoiceItems = new QuestInfoChoiceItem[SharedConst.QuestRewardChoicesCount];

    public QuestInfo()
    {
        LogTitle = "";
        LogDescription = "";
        QuestDescription = "";
        AreaDescription = "";
        PortraitGiverText = "";
        PortraitGiverName = "";
        PortraitTurnInText = "";
        PortraitTurnInName = "";
        QuestCompletionLog = "";
    }
}