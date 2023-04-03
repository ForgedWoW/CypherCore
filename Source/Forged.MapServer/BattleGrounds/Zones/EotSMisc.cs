// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct EotSMisc
{
    public const uint EventStartBattle = 13180; // Achievement: Flurry
    public const uint ExploitTeleportLocationAlliance = 3773;
    public const uint ExploitTeleportLocationHorde = 3772;
    public const uint EYWeekendHonorTicks = 160;
    public const int FlagRespawnTime = (8 * Time.IN_MILLISECONDS);
    public const int FPointsTickTime = (2 * Time.IN_MILLISECONDS);

    public const uint NotEYWeekendHonorTicks = 260;
    public const uint ObjectiveCaptureFlag = 183;

    public const uint SpellNetherstormFlag = 34976;
    public const uint SpellPlayerDroppedFlag = 34991;

    public static uint[] FlagPoints =
    {
        75, 85, 100, 500
    };

    public static BattlegroundEYCapturingPointStruct[] m_CapturingPointTypes =
    {
        new(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectTypes.ABannerFelReaverCenter, EotSBroadcastTexts.AllianceTakenFelReaverRuins, EotSObjectTypes.HBannerFelReaverCenter, EotSBroadcastTexts.HordeTakenFelReaverRuins, EotSGaveyardIds.FelReaver), new(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectTypes.ABannerBloodElfCenter, EotSBroadcastTexts.AllianceTakenBloodElfTower, EotSObjectTypes.HBannerBloodElfCenter, EotSBroadcastTexts.HordeTakenBloodElfTower, EotSGaveyardIds.BloodElf), new(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSBroadcastTexts.AllianceTakenDraeneiRuins, EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSBroadcastTexts.HordeTakenDraeneiRuins, EotSGaveyardIds.DraeneiRuins), new(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectTypes.ABannerMageTowerCenter, EotSBroadcastTexts.AllianceTakenMageTower, EotSObjectTypes.HBannerMageTowerCenter, EotSBroadcastTexts.HordeTakenMageTower, EotSGaveyardIds.MageTower)
    };

    public static BattlegroundEYLosingPointStruct[] m_LosingPointTypes =
    {
        new(EotSObjectTypes.NBannerFelReaverCenter, EotSObjectTypes.ABannerFelReaverCenter, EotSBroadcastTexts.AllianceLostFelReaverRuins, EotSObjectTypes.HBannerFelReaverCenter, EotSBroadcastTexts.HordeLostFelReaverRuins), new(EotSObjectTypes.NBannerBloodElfCenter, EotSObjectTypes.ABannerBloodElfCenter, EotSBroadcastTexts.AllianceLostBloodElfTower, EotSObjectTypes.HBannerBloodElfCenter, EotSBroadcastTexts.HordeLostBloodElfTower), new(EotSObjectTypes.NBannerDraeneiRuinsCenter, EotSObjectTypes.ABannerDraeneiRuinsCenter, EotSBroadcastTexts.AllianceLostDraeneiRuins, EotSObjectTypes.HBannerDraeneiRuinsCenter, EotSBroadcastTexts.HordeLostDraeneiRuins), new(EotSObjectTypes.NBannerMageTowerCenter, EotSObjectTypes.ABannerMageTowerCenter, EotSBroadcastTexts.AllianceLostMageTower, EotSObjectTypes.HBannerMageTowerCenter, EotSBroadcastTexts.HordeLostMageTower)
    };

    public static BattlegroundEYPointIconsStruct[] m_PointsIconStruct =
    {
        new(EotSWorldStateIds.FelReaverUncontrol, EotSWorldStateIds.FelReaverAllianceControl, EotSWorldStateIds.FelReaverHordeControl, EotSWorldStateIds.FelReaverAllianceControlState, EotSWorldStateIds.FelReaverHordeControlState), new(EotSWorldStateIds.BloodElfUncontrol, EotSWorldStateIds.BloodElfAllianceControl, EotSWorldStateIds.BloodElfHordeControl, EotSWorldStateIds.BloodElfAllianceControlState, EotSWorldStateIds.BloodElfHordeControlState), new(EotSWorldStateIds.DraeneiRuinsUncontrol, EotSWorldStateIds.DraeneiRuinsAllianceControl, EotSWorldStateIds.DraeneiRuinsHordeControl, EotSWorldStateIds.DraeneiRuinsAllianceControlState, EotSWorldStateIds.DraeneiRuinsHordeControlState), new(EotSWorldStateIds.MageTowerUncontrol, EotSWorldStateIds.MageTowerAllianceControl, EotSWorldStateIds.MageTowerHordeControl, EotSWorldStateIds.MageTowerAllianceControlState, EotSWorldStateIds.MageTowerHordeControlState)
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