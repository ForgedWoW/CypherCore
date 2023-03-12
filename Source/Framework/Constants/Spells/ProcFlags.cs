// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum ProcFlags : uint
{
	None = 0x0,

	Heartbeat = 0x01, // 00 Killed by agressor - not sure about this flag
	Kill = 0x02,      // 01 Kill target (in most cases need XP/Honor reward)

	DealMeleeSwing = 0x04, // 02 Done melee auto attack
	TakeMeleeSwing = 0x08, // 03 Taken melee auto attack

	DealMeleeAbility = 0x10, // 04 Done attack by Spell that has dmg class melee
	TakeMeleeAbility = 0x20, // 05 Taken attack by Spell that has dmg class melee

	DealRangedAttack = 0x40, // 06 Done ranged auto attack
	TakeRangedAttack = 0x80, // 07 Taken ranged auto attack

	DealRangedAbility = 0x100, // 08 Done attack by Spell that has dmg class ranged
	TakeRangedAbility = 0x200, // 09 Taken attack by Spell that has dmg class ranged

	DealHelpfulAbility = 0x400, // 10 Done positive spell that has dmg class none
	TakeHelpfulAbility = 0x800, // 11 Taken positive spell that has dmg class none

	DealHarmfulAbility = 0x1000, // 12 Done negative spell that has dmg class none
	TakeHarmfulAbility = 0x2000, // 13 Taken negative spell that has dmg class none

	DealHelpfulSpell = 0x4000, // 14 Done positive spell that has dmg class magic
	TakeHelpfulSpell = 0x8000, // 15 Taken positive spell that has dmg class magic

	DealHarmfulSpell = 0x10000, // 16 Done negative spell that has dmg class magic
	TakeHarmfulSpell = 0x20000, // 17 Taken negative spell that has dmg class magic

	DealHarmfulPeriodic = 0x40000, // 18 Successful do periodic damage
	TakeHarmfulPeriodic = 0x80000, // 19 Taken spell periodic damage

	TakeAnyDamage = 0x100000, // 20 Taken any damage

	DealHelpfulPeriodic = 0x200000, // 21 On trap activation (possibly needs name change to ONGAMEOBJECTCAST or USE)

	MainHandWeaponSwing = 0x400000, // 22 Done main-hand melee attacks (spell and autoattack)
	OffHandWeaponSwing = 0x800000,  // 23 Done off-hand melee attacks (spell and autoattack)

	Death = 0x1000000, // 24 Died in any way
	Jump = 0x02000000, // 25 Jumped

	CloneSpell = 0x4000000, // 26 Proc Clone Spell

	EnterCombat = 0x08000000,    // 27 Entered combat
	EncounterStart = 0x10000000, // 28 Encounter started

	CastEnded = 0x20000000, // 29 Cast Ended
	Looted = 0x40000000,    // 30 Looted (took from loot, not opened loot window)

	TakeHelpfulPeriodic = 0x80000000, // 31 Take Helpful Periodic

	// flag masks
	AutoAttackMask = DealMeleeSwing | TakeMeleeSwing | DealRangedAttack | TakeRangedAttack,

	MeleeMask = DealMeleeSwing | TakeMeleeSwing | DealMeleeAbility | TakeMeleeAbility | MainHandWeaponSwing | OffHandWeaponSwing,

	RangedMask = DealRangedAttack | TakeRangedAttack | DealRangedAbility | TakeRangedAbility,

	SpellMask = DealMeleeAbility | TakeMeleeAbility | DealRangedAttack | TakeRangedAttack | DealRangedAbility | TakeRangedAbility | DealHelpfulAbility | TakeHelpfulAbility | DealHarmfulAbility | TakeHarmfulAbility | DealHelpfulSpell | TakeHelpfulSpell | DealHarmfulSpell | TakeHarmfulSpell | DealHarmfulPeriodic | TakeHarmfulPeriodic | DealHelpfulPeriodic | TakeHelpfulPeriodic,

	DoneHitMask = DealMeleeSwing | DealRangedAttack | DealMeleeAbility | DealRangedAbility | DealHelpfulAbility | DealHarmfulAbility | DealHelpfulSpell | DealHarmfulSpell | DealHarmfulPeriodic | DealHelpfulPeriodic | MainHandWeaponSwing | OffHandWeaponSwing,

	TakenHitMask = TakeMeleeSwing | TakeRangedAttack | TakeMeleeAbility | TakeRangedAbility | TakeHelpfulAbility | TakeHarmfulAbility | TakeHelpfulSpell | TakeHarmfulSpell | TakeHarmfulPeriodic | TakeAnyDamage,

	ReqSpellPhaseMask = SpellMask & DoneHitMask,

	MeleeBasedTriggerMask = (DealMeleeSwing |
							TakeMeleeSwing |
							DealMeleeAbility |
							TakeMeleeAbility |
							DealRangedAttack |
							TakeRangedAttack |
							DealRangedAbility |
							TakeRangedAbility)
}