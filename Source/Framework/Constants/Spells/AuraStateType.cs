// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum AuraStateType
{
	// (C) used in caster aura state     (T) used in target aura state
	// (c) used in caster aura state-not (t) used in target aura state-not
	None = 0,               // C   |
	Defensive = 1,          // Cctt|
	Wounded20Percent = 2,   // Cct |
	Unbalanced = 3,         // Cct | Nyi
	Frozen = 4,             //  C T|
	Marked = 5,             // C  T| Nyi
	Wounded25Percent = 6,   //   T |
	Defensive2 = 7,         // Cc  | Nyi
	Banished = 8,           //  C  | Nyi
	Dazed = 9,              //    T|
	Victorious = 10,        // C   |
	Rampage = 11,           //     | Nyi
	FaerieFire = 12,        //  C T|
	Wounded35Percent = 13,  // Cct |
	RaidEncounter2 = 14,    //  Ct |
	DruidPeriodicHeal = 15, //   T |
	RoguePoisoned = 16,     //     |
	Enraged = 17,           // C   |
	Bleed = 18,             //   T |
	Vulnerable = 19,        //     | Nyi
	ArenaPreparation = 20,  //  C  |
	WoundHealth20_80 = 21,  //   T |
	RaidEncounter = 22,     // Cctt|
	Healthy75Percent = 23,  // C   |
	WoundHealth35_80 = 24,  //   T |
	Max,

	PerCasterAuraStateMask = (1 << (RaidEncounter2 - 1)) | (1 << (RoguePoisoned - 1))
}