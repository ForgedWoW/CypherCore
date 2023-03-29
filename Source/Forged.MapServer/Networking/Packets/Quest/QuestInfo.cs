// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Quest;
using Framework.Constants;

namespace Forged.MapServer.Networking.Packets.Quest;

public class QuestInfo
{
    public uint QuestID;
    public int QuestType; // Accepted values: 0, 1 or 2. 0 == IsAutoComplete() (skip objectives/details)
    public uint ContentTuningID;
    public uint QuestPackageID;
    public int QuestSortID; // zone or sort to display in quest log
    public uint QuestInfoID;
    public uint SuggestedGroupNum;
    public uint RewardNextQuest;    // client will request this quest from NPC, if not 0
    public uint RewardXPDifficulty; // used for calculating rewarded experience
    public float RewardXPMultiplier = 1.0f;
    public int RewardMoney; // reward money (below max lvl)
    public uint RewardMoneyDifficulty;
    public float RewardMoneyMultiplier = 1.0f;
    public uint RewardBonusMoney;
    public List<QuestCompleteDisplaySpell> RewardDisplaySpell = new(); // reward spell, this spell will be displayed (icon)
    public uint RewardSpell;
    public uint RewardHonor;
    public float RewardKillHonor;
    public int RewardArtifactXPDifficulty;
    public float RewardArtifactXPMultiplier;
    public int RewardArtifactCategoryID;
    public uint StartItem;
    public uint Flags;
    public uint FlagsEx;
    public uint FlagsEx2;
    public uint POIContinent;
    public float POIx;
    public float POIy;
    public uint POIPriority;
    public long AllowableRaces = -1;
    public string LogTitle;
    public string LogDescription;
    public string QuestDescription;
    public string AreaDescription;
    public uint RewardTitle; // new 2.4.0, player gets this title (id from CharTitles)
    public int RewardArenaPoints;
    public uint RewardSkillLineID; // reward skill id
    public uint RewardNumSkillUps; // reward skill points
    public uint PortraitGiver;     // quest giver entry ?
    public uint PortraitGiverMount;
    public int PortraitGiverModelSceneID;
    public uint PortraitTurnIn; // quest turn in entry ?
    public string PortraitGiverText;
    public string PortraitGiverName;
    public string PortraitTurnInText;
    public string PortraitTurnInName;
    public string QuestCompletionLog;
    public uint RewardFactionFlags; // rep mask (unsure on what it does)
    public uint AcceptedSoundKitID;
    public uint CompleteSoundKitID;
    public uint AreaGroupID;
    public uint TimeAllowed;
    public int TreasurePickerID;
    public int Expansion;
    public int ManagedWorldStateID;
    public int QuestSessionBonus;
    public int QuestGiverCreatureID; // used to select ConditionalQuestText
    public List<QuestObjective> Objectives = new();
    public List<ConditionalQuestText> ConditionalQuestDescription = new();
    public List<ConditionalQuestText> ConditionalQuestCompletionLog = new();
    public uint[] RewardItems = new uint[SharedConst.QuestRewardItemCount];
    public uint[] RewardAmount = new uint[SharedConst.QuestRewardItemCount];
    public int[] ItemDrop = new int[SharedConst.QuestItemDropCount];
    public int[] ItemDropQuantity = new int[SharedConst.QuestItemDropCount];
    public QuestInfoChoiceItem[] UnfilteredChoiceItems = new QuestInfoChoiceItem[SharedConst.QuestRewardChoicesCount];
    public uint[] RewardFactionID = new uint[SharedConst.QuestRewardReputationsCount];
    public int[] RewardFactionValue = new int[SharedConst.QuestRewardReputationsCount];
    public int[] RewardFactionOverride = new int[SharedConst.QuestRewardReputationsCount];
    public int[] RewardFactionCapIn = new int[SharedConst.QuestRewardReputationsCount];
    public uint[] RewardCurrencyID = new uint[SharedConst.QuestRewardCurrencyCount];
    public uint[] RewardCurrencyQty = new uint[SharedConst.QuestRewardCurrencyCount];
    public bool ReadyForTranslation;

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