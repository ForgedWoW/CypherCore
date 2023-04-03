// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.MapServer.BattleGrounds.Zones;

internal struct EotSWorldStateIds
{
    public const uint AllianceBase = 2752;
    public const uint AllianceResources = 1776;
    public const uint BloodElfAllianceControl = 2723;
    public const uint BloodElfAllianceControlState = 17365;
    public const uint BloodElfHordeControl = 2724;
    public const uint BloodElfHordeControlState = 17363;
    public const uint BloodElfUncontrol = 2722;
    public const uint DraeneiRuinsAllianceControl = 2732;
    public const uint DraeneiRuinsAllianceControlState = 17366;
    public const uint DraeneiRuinsHordeControl = 2733;
    public const uint DraeneiRuinsHordeControlState = 17362;
    public const uint DraeneiRuinsUncontrol = 2731;
    public const uint FelReaverAllianceControl = 2726;
    public const uint FelReaverAllianceControlState = 17367;
    public const uint FelReaverHordeControl = 2727;
    public const uint FelReaverHordeControlState = 17364;
    public const uint FelReaverUncontrol = 2725;
    public const uint HordeBase = 2753;
    public const uint HordeResources = 1777;
    public const uint MageTowerAllianceControl = 2730;
    public const uint MageTowerAllianceControlState = 17368;
    public const uint MageTowerHordeControl = 2729;
    public const uint MageTowerHordeControlState = 17361;
    public const uint MageTowerUncontrol = 2728;
    public const uint MaxResources = 1780;

    public const uint NetherstormFlag = 8863;

    //Set To 2 When Flag Is Picked Up; And To 1 If It Is Dropped
    public const uint NetherstormFlagStateAlliance = 9808;

    public const uint NetherstormFlagStateHorde = 9809;
    public const uint ProgressBarPercentGrey = 2720; //100 = Empty (Only Grey); 0 = Blue|Red (No Grey)
    public const uint ProgressBarShow = 2718;

    public const uint ProgressBarStatus = 2719; //50 Init!; 48 ... Hordak Bere .. 33 .. 0 = Full 100% Hordacky; 100 = Full Alliance
    //1 Init; 0 Druhy Send - Bez Messagu; 1 = Controlled Aliance
}