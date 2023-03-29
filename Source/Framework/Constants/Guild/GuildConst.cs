// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public class GuildConst
{
    public const int MaxBankTabs = 8;
    public const int MaxBankSlots = 98;
    public const int BankMoneyLogsTab = 100;

    public const ulong MoneyLimit = 100000000000;
    public const uint WithdrawMoneyUnlimited = 0xFFFFFFFF;
    public const int WithdrawSlotUnlimited = -1;
    public const uint EventLogGuidUndefined = 0xFFFFFFFF;

    public const uint ChallengesTypes = 6;
    public const uint CharterItemId = 5863;

    public const int RankNone = 0xFF;
    public const int MinRanks = 5;
    public const int MaxRanks = 10;

    public const int BankLogMaxRecords = 25;
    public const int EventLogMaxRecords = 100;
    public const int NewsLogMaxRecords = 250;

    public static int[] ChallengeGoldReward =
    {
        0, 250, 1000, 500, 250, 500
    };

    public static int[] ChallengeMaxLevelGoldReward =
    {
        0, 125, 500, 250, 125, 250
    };

    public static int[] ChallengesMaxCount =
    {
        0, 7, 1, 3, 0, 3
    };

    public static uint MinNewsItemLevel = 353;

    public static byte OldMaxLevel = 25;

    public static uint MasterDethroneInactiveDays = 90;
}