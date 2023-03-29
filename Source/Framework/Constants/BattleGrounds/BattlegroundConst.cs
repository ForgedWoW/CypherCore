// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public struct BattlegroundConst
{
    //Time Intervals
    public const uint CheckPlayerPositionInverval = 1000; // Ms

    public const uint ResurrectionInterval = 30000; // Ms

    //RemindInterval                 = 10000,               // Ms
    public const uint InvitationRemindTime = 20000;        // Ms
    public const uint InviteAcceptWaitTime = 90000;        // Ms
    public const uint AutocloseBattleground = 120000;      // Ms
    public const uint MaxOfflineTime = 300;                // Secs
    public const uint RespawnOneDay = 86400;               // Secs
    public const uint RespawnImmediately = 0;              // Secs
    public const uint BuffRespawnTime = 180;               // Secs
    public const uint BattlegroundCountdownMax = 120;      // Secs
    public const uint ArenaCountdownMax = 60;              // Secs
    public const uint PlayerPositionUpdateInterval = 5000; // Ms

    //EventIds
    public const int EventIdFirst = 0;
    public const int EventIdSecond = 1;
    public const int EventIdThird = 2;
    public const int EventIdFourth = 3;
    public const int EventIdCount = 4;

    //Quests
    public const uint WsQuestReward = 43483;
    public const uint AbQuestReward = 43484;
    public const uint AvQuestReward = 43475;
    public const uint AvQuestKilledBoss = 23658;
    public const uint EyQuestReward = 43477;
    public const uint SaQuestReward = 61213;
    public const uint AbQuestReward4Bases = 24061;
    public const uint AbQuestReward5Bases = 24064;

    //BuffObjects
    public const uint SpeedBuff = 179871;
    public const uint RegenBuff = 179904;
    public const uint BerserkerBuff = 179905;

    //QueueGroupTypes
    public const uint BgQueuePremadeAlliance = 0;
    public const uint BgQueuePremadeHorde = 1;
    public const uint BgQueueNormalAlliance = 2;
    public const uint BgQueueNormalHorde = 3;
    public const int BgQueueTypesCount = 4;

    //PlayerPosition
    public const sbyte PlayerPositionIconNone = 0;
    public const sbyte PlayerPositionIconHordeFlag = 1;
    public const sbyte PlayerPositionIconAllianceFlag = 2;

    public const sbyte PlayerPositionArenaSlotNone = 1;
    public const sbyte PlayerPositionArenaSlot1 = 2;
    public const sbyte PlayerPositionArenaSlot2 = 3;
    public const sbyte PlayerPositionArenaSlot3 = 4;
    public const sbyte PlayerPositionArenaSlot4 = 5;
    public const sbyte PlayerPositionArenaSlot5 = 6;

    //Spells
    public const uint SpellWaitingForResurrect = 2584; // Waiting To Resurrect
    public const uint SpellSpiritHealChannel = 22011;  // Spirit Heal Channel
    public const uint SpellSpiritHealChannelVisual = 3060;
    public const uint SpellSpiritHeal = 22012;           // Spirit Heal
    public const uint SpellResurrectionVisual = 24171;   // Resurrection Impact Visual
    public const uint SpellArenaPreparation = 32727;     // Use This One, 32728 Not Correct
    public const uint SpellPreparation = 44521;          // Preparation
    public const uint SpellSpiritHealMana = 44535;       // Spirit Heal
    public const uint SpellRecentlyDroppedFlag = 42792;  // Recently Dropped Flag
    public const uint SpellAuraPlayerInactive = 43681;   // Inactive
    public const uint SpellHonorableDefender25y = 68652; // +50% Honor When Standing At A Capture Point That You Control, 25yards Radius (Added In 3.2)
    public const uint SpellHonorableDefender60y = 66157; // +50% Honor When Standing At A Capture Point That You Control, 60yards Radius (Added In 3.2), Probably For 40+ Player Battlegrounds
    public const uint SpellMercenaryContractHorde = 193472;
    public const uint SpellMercenaryContractAlliance = 193475;
    public const uint SpellMercenaryHorde1 = 193864;
    public const uint SpellMercenaryHordeReactions = 195838;
    public const uint SpellMercenaryAlliance1 = 193863;
    public const uint SpellMercenaryAllianceReactions = 195843;
    public const uint SpellMercenaryShapeshift = 193970;
}

// indexes of BattlemasterList.dbc

//Arenas