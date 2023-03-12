// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum BattlePetState
{
	MaxHealthBonus = 2,
	InternalInitialLevel = 17,
	StatPower = 18,
	StatStamina = 19,
	StatSpeed = 20,
	ModDamageDealtPercent = 23,
	Gender = 78, // 1 - Male, 2 - Female
	CosmeticWaterBubbled = 85,
	SpecialIsCockroach = 93,
	CosmeticFlyTier = 128,
	CosmeticBigglesworth = 144,
	PassiveElite = 153,
	PassiveBoss = 162,
	CosmeticTreasureGoblin = 176,

	// These Are Not In Battlepetstate.Db2 But Are Used In Battlepetspeciesstate.Db2
	StartWithBuff = 183,
	StartWithBuff2 = 184,

	//
	CosmeticSpectralBlue = 196
}