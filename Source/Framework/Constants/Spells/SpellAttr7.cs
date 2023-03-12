// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellAttr7 : uint
{
	Unk0 = 0x01,                            //  0 Shaman'S New Spells (Call Of The ...), Feign Death.
	IgnoreDurationMods = 0x02,              //  1 Duration is not affected by duration modifiers
	ReactivateAtResurrect = 0x04,           //  2 Paladin'S Auras And 65607 Only.
	IsCheatSpell = 0x08,                    //  3 Cannot Cast If Caster Doesn'T Have Unitflag2 & UnitFlag2AllowCheatSpells
	Unk4 = 0x10,                            //  4 Only 47883 (Soulstone Resurrection) And Test Spell.
	SummonTotem = 0x20,                     //  5 Only Shaman Player Totems.
	NoPushbackOnDamage = 0x40,              //  6 Does not cause spell pushback on damage
	Unk7 = 0x80,                            //  7 66218 (Launch) Spell.
	HordeOnly = 0x100,                      //  8 Teleports, Mounts And Other Spells.
	AllianceOnly = 0x200,                   //  9 Teleports, Mounts And Other Spells.
	DispelCharges = 0x400,                  // 10 Dispel And Spellsteal Individual Charges Instead Of Whole Aura.
	InterruptOnlyNonplayer = 0x800,         // 11 Only Non-Player Casts Interrupt, Though Feral Charge - Bear Has It.
	SilenceOnlyNonplayer = 0x1000,          // 12 Not Set In 3.2.2a.
	CanAlwaysBeInterrupted = 0x2000,        // 13 Can always be interrupted, even if caster is immune
	Unk14 = 0x4000,                         // 14 Only 52150 (Raise Dead - Pet) Spell.
	Unk15 = 0x8000,                         // 15 Exorcism. Usable On Players? 100% Crit Chance On Undead And Demons?
	HiddenInSpellbookWhenLearned = 0x10000, // 16 After learning these spells become hidden in spellbook (but are visible when not learned for low level characters)
	Unk17 = 0x20000,                        // 17 Only 27965 (Suicide) Spell.
	HasChargeEffect = 0x40000,              // 18 Only Spells That Have Charge Among Effects.
	ZoneTeleport = 0x80000,                 // 19 Teleports To Specific Zones.
	Unk20 = 0x100000,                       // 20 Blink, Divine Shield, Ice Block
	Unk21 = 0x200000,                       // 21 Not Set
	Unk22 = 0x400000,                       // 22
	NoAttackDodge = 0x800000,               // 23 No Attack Dodge
	NoAttackParry = 0x1000000,              // 24 No Attack Parry
	NoAttackMiss = 0x2000000,               // No Attack Miss
	Unk26 = 0x4000000,                      // 26
	BypassNoResurrectAura = 0x8000000,      // 27 Bypass No Resurrect Aura
	ConsolidatedRaidBuff = 0x10000000,      // 28 Related To Player Positive Buff
	Unk29 = 0x20000000,                     // 29 Only 69028, 71237
	Unk30 = 0x40000000,                     // 30 Burning Determination, Divine Sacrifice, Earth Shield, Prayer Of Mending
	ClientIndicator = 0x80000000            // 31 Only 70769
}