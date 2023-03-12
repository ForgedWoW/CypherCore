// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum WorldStates
{
	CurrentPvpSeasonId = 3191,
	PreviousPvpSeasonId = 3901,

	TeamInInstanceAlliance = 4485,
	TeamInInstanceHorde = 4486,

	BattlefieldWgVehicleH = 3490,
	BattlefieldWgMaxVehicleH = 3491,
	BattlefieldWgVehicleA = 3680,
	BattlefieldWgMaxVehicleA = 3681,
	BattlefieldWgWorkshopKW = 3698,
	BattlefieldWgWorkshopKE = 3699,
	BattlefieldWgWorkshopNw = 3700,
	BattlefieldWgWorkshopNe = 3701,
	BattlefieldWgWorkshopSw = 3702,
	BattlefieldWgWorkshopSe = 3703,
	BattlefieldWgShowTimeBattleEnd = 3710,
	BattlefieldWgTimeBattleEnd = 3781,
	BattlefieldWgShowTimeNextBattle = 3801,
	BattlefieldWgDefender = 3802,
	BattlefieldWgAttacker = 3803,
	BattlefieldWgAttackedH = 4022,
	BattlefieldWgAttackedA = 4023,
	BattlefieldWgDefendedH = 4024,
	BattlefieldWgDefendedA = 4025,
	BattlefieldWgTimeNextBattle = 4354,

	BattlefieldTbAllianceControlsShow = 5385,
	BattlefieldTbHordeControlsShow = 5384,
	BattlefieldTbAllianceAttackingShow = 5546,
	BattlefieldTbHordeAttackingShow = 5547,

	BattlefieldTbBuildingsCaptured = 5348,
	BattlefieldTbBuildingsCapturedShow = 5349,
	BattlefieldTbTowersDestroyed = 5347,
	BattlefieldTbTowersDestroyedShow = 5350,

	BattlefieldTbFactionControlling = 5334, // 1 -> Alliance, 2 -> Horde

	BattlefieldTbTimeNextBattle = 5332,
	BattlefieldTbTimeNextBattleShow = 5387,
	BattlefieldTbTimeBattleEnd = 5333,
	BattlefieldTbTimeBattleEndShow = 5346,

	BattlefieldTbStatePreparations = 5684,
	BattlefieldTbStateBattle = 5344,

	BattlefieldTbKeepHorde = 5469,
	BattlefieldTbKeepAlliance = 5470,

	BattlefieldTbGarrisonHordeControlled = 5418,
	BattlefieldTbGarrisonHordeCapturing = 5419,
	BattlefieldTbGarrisonNeutral = 5420, // Unused
	BattlefieldTbGarrisonAllianceCapturing = 5421,
	BattlefieldTbGarrisonAllianceControlled = 5422,

	BattlefieldTbVigilHordeControlled = 5423,
	BattlefieldTbVigilHordeCapturing = 5424,
	BattlefieldTbVigilNeutral = 5425, // Unused
	BattlefieldTbVigilAllianceCapturing = 5426,
	BattlefieldTbVigilAllianceControlled = 5427,

	BattlefieldTbSlagworksHordeControlled = 5428,
	BattlefieldTbSlagworksHordeCapturing = 5429,
	BattlefieldTbSlagworksNeutral = 5430, // Unused
	BattlefieldTbSlagworksAllianceCapturing = 5431,
	BattlefieldTbSlagworksAllianceControlled = 5432,

	BattlefieldTbWestIntactHorde = 5433,
	BattlefieldTbWestDamagedHorde = 5434,
	BattlefieldTbWestDestroyedNeutral = 5435,
	BattlefieldTbWestIntactAlliance = 5436,
	BattlefieldTbWestDamagedAlliance = 5437,
	BattlefieldTbWestIntactNeutral = 5453,  // Unused
	BattlefieldTbWestDamagedNeutral = 5454, // Unused

	BattlefieldTbSouthIntactHorde = 5438,
	BattlefieldTbSouthDamagedHorde = 5439,
	BattlefieldTbSouthDestroyedNeutral = 5440,
	BattlefieldTbSouthIntactAlliance = 5441,
	BattlefieldTbSouthDamagedAlliance = 5442,
	BattlefieldTbSouthIntactNeutral = 5455,  // Unused
	BattlefieldTbSouthDamagedNeutral = 5456, // Unused

	BattlefieldTbEastIntactHorde = 5443,
	BattlefieldTbEastDamagedHorde = 5444,
	BattlefieldTbEastDestroyedNeutral = 5445,
	BattlefieldTbEastIntactAlliance = 5446,
	BattlefieldTbEastDamagedAlliance = 5447,
	BattlefieldTbEastIntactNeutral = 5451,
	BattlefieldTbEastDamagedNeutral = 5452,

	WarModeHordeBuffValue = 17042,
	WarModeAllianceBuffValue = 17043,
}