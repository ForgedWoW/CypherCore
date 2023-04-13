// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct EotSMisc
{
    public const uint EVENT_START_BATTLE = 13180; // Achievement: Flurry
    public const uint EXPLOIT_TELEPORT_LOCATION_ALLIANCE = 3773;
    public const uint EXPLOIT_TELEPORT_LOCATION_HORDE = 3772;
    public const uint EY_WEEKEND_HONOR_TICKS = 160;
    public const int FLAG_RESPAWN_TIME = 8 * Time.IN_MILLISECONDS;
    public const int F_POINTS_TICK_TIME = 2 * Time.IN_MILLISECONDS;

    public const uint NOT_EY_WEEKEND_HONOR_TICKS = 260;
    public const uint OBJECTIVE_CAPTURE_FLAG = 183;

    public const uint SPELL_NETHERSTORM_FLAG = 34976;
    public const uint SPELL_PLAYER_DROPPED_FLAG = 34991;

    public static uint[] FlagPoints =
    {
        75, 85, 100, 500
    };

    public static BattlegroundEyCapturingPointStruct[] MCapturingPointTypes =
    {
        new(EotSObjectTypes.N_BANNER_FEL_REAVER_CENTER, EotSObjectTypes.A_BANNER_FEL_REAVER_CENTER, EotSBroadcastTexts.ALLIANCE_TAKEN_FEL_REAVER_RUINS, EotSObjectTypes.H_BANNER_FEL_REAVER_CENTER, EotSBroadcastTexts.HORDE_TAKEN_FEL_REAVER_RUINS, EotSGaveyardIds.FEL_REAVER), new(EotSObjectTypes.N_BANNER_BLOOD_ELF_CENTER, EotSObjectTypes.A_BANNER_BLOOD_ELF_CENTER, EotSBroadcastTexts.ALLIANCE_TAKEN_BLOOD_ELF_TOWER, EotSObjectTypes.H_BANNER_BLOOD_ELF_CENTER, EotSBroadcastTexts.HORDE_TAKEN_BLOOD_ELF_TOWER, EotSGaveyardIds.BLOOD_ELF), new(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_CENTER, EotSObjectTypes.A_BANNER_DRAENEI_RUINS_CENTER, EotSBroadcastTexts.ALLIANCE_TAKEN_DRAENEI_RUINS, EotSObjectTypes.H_BANNER_DRAENEI_RUINS_CENTER, EotSBroadcastTexts.HORDE_TAKEN_DRAENEI_RUINS, EotSGaveyardIds.DRAENEI_RUINS), new(EotSObjectTypes.N_BANNER_MAGE_TOWER_CENTER, EotSObjectTypes.A_BANNER_MAGE_TOWER_CENTER, EotSBroadcastTexts.ALLIANCE_TAKEN_MAGE_TOWER, EotSObjectTypes.H_BANNER_MAGE_TOWER_CENTER, EotSBroadcastTexts.HORDE_TAKEN_MAGE_TOWER, EotSGaveyardIds.MAGE_TOWER)
    };

    public static BattlegroundEyLosingPointStruct[] MLosingPointTypes =
    {
        new(EotSObjectTypes.N_BANNER_FEL_REAVER_CENTER, EotSObjectTypes.A_BANNER_FEL_REAVER_CENTER, EotSBroadcastTexts.ALLIANCE_LOST_FEL_REAVER_RUINS, EotSObjectTypes.H_BANNER_FEL_REAVER_CENTER, EotSBroadcastTexts.HORDE_LOST_FEL_REAVER_RUINS), new(EotSObjectTypes.N_BANNER_BLOOD_ELF_CENTER, EotSObjectTypes.A_BANNER_BLOOD_ELF_CENTER, EotSBroadcastTexts.ALLIANCE_LOST_BLOOD_ELF_TOWER, EotSObjectTypes.H_BANNER_BLOOD_ELF_CENTER, EotSBroadcastTexts.HORDE_LOST_BLOOD_ELF_TOWER), new(EotSObjectTypes.N_BANNER_DRAENEI_RUINS_CENTER, EotSObjectTypes.A_BANNER_DRAENEI_RUINS_CENTER, EotSBroadcastTexts.ALLIANCE_LOST_DRAENEI_RUINS, EotSObjectTypes.H_BANNER_DRAENEI_RUINS_CENTER, EotSBroadcastTexts.HORDE_LOST_DRAENEI_RUINS), new(EotSObjectTypes.N_BANNER_MAGE_TOWER_CENTER, EotSObjectTypes.A_BANNER_MAGE_TOWER_CENTER, EotSBroadcastTexts.ALLIANCE_LOST_MAGE_TOWER, EotSObjectTypes.H_BANNER_MAGE_TOWER_CENTER, EotSBroadcastTexts.HORDE_LOST_MAGE_TOWER)
    };

    public static BattlegroundEyPointIconsStruct[] MPointsIconStruct =
    {
        new(EotSWorldStateIds.FEL_REAVER_UNCONTROL, EotSWorldStateIds.FEL_REAVER_ALLIANCE_CONTROL, EotSWorldStateIds.FEL_REAVER_HORDE_CONTROL, EotSWorldStateIds.FEL_REAVER_ALLIANCE_CONTROL_STATE, EotSWorldStateIds.FEL_REAVER_HORDE_CONTROL_STATE), new(EotSWorldStateIds.BLOOD_ELF_UNCONTROL, EotSWorldStateIds.BLOOD_ELF_ALLIANCE_CONTROL, EotSWorldStateIds.BLOOD_ELF_HORDE_CONTROL, EotSWorldStateIds.BLOOD_ELF_ALLIANCE_CONTROL_STATE, EotSWorldStateIds.BLOOD_ELF_HORDE_CONTROL_STATE), new(EotSWorldStateIds.DRAENEI_RUINS_UNCONTROL, EotSWorldStateIds.DRAENEI_RUINS_ALLIANCE_CONTROL, EotSWorldStateIds.DRAENEI_RUINS_HORDE_CONTROL, EotSWorldStateIds.DRAENEI_RUINS_ALLIANCE_CONTROL_STATE, EotSWorldStateIds.DRAENEI_RUINS_HORDE_CONTROL_STATE), new(EotSWorldStateIds.MAGE_TOWER_UNCONTROL, EotSWorldStateIds.MAGE_TOWER_ALLIANCE_CONTROL, EotSWorldStateIds.MAGE_TOWER_HORDE_CONTROL, EotSWorldStateIds.MAGE_TOWER_ALLIANCE_CONTROL_STATE, EotSWorldStateIds.MAGE_TOWER_HORDE_CONTROL_STATE)
    };

    public static byte[] TickPoints =
    {
        1, 2, 5, 10
    };

    public static Position[] TriggerPositions =
    {
        new(2044.28f, 1729.68f, 1189.96f, 0.017453f), // FEL_REAVER center
        new(2048.83f, 1393.65f, 1194.49f, 0.20944f),  // BLOOD_ELF center
        new(2286.56f, 1402.36f, 1197.11f, 3.72381f),  // DRAENEI_RUINS center
        new(2284.48f, 1731.23f, 1189.99f, 2.89725f)   // MAGE_TOWER center
    };
}