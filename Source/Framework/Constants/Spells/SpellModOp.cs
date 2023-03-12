// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellModOp
{
	HealingAndDamage = 0,
	Duration = 1,
	Hate = 2,
	PointsIndex0 = 3,
	ProcCharges = 4,
	Range = 5,
	Radius = 6,
	CritChance = 7,
	Points = 8,
	ResistPushback = 9,
	ChangeCastTime = 10,
	Cooldown = 11,
	PointsIndex1 = 12,
	TargetResistance = 13,
	PowerCost0 = 14, // Used when SpellPowerEntry::PowerIndex == 0
	CritDamageAndHealing = 15,
	HitChance = 16,
	ChainTargets = 17,
	ProcChance = 18,
	Period = 19,
	ChainAmplitude = 20,
	StartCooldown = 21,
	PeriodicHealingAndDamage = 22,
	PointsIndex2 = 23,
	BonusCoefficient = 24,
	TriggerDamage = 25, // NYI
	ProcFrequency = 26,
	Amplitude = 27,
	DispelResistance = 28,
	CrowdDamage = 29, // NYI
	PowerCostOnMiss = 30,
	Doses = 31,
	PointsIndex3 = 32,
	PointsIndex4 = 33,
	PowerCost1 = 34, // Used when SpellPowerEntry::PowerIndex == 1
	ChainJumpDistance = 35,
	AreaTriggerMaxSummons = 36, // NYI
	MaxAuraStacks = 37,
	ProcCooldown = 38,
	PowerCost2 = 39, // Used when SpellPowerEntry::PowerIndex == 2

	Max = 40
}