// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellAttr11 : uint
{
	Unk0 = 0x01,                        //  0
	Unk1 = 0x02,                        //  1
	ScalesWithItemLevel = 0x04,         //  2
	Unk3 = 0x08,                        //  3
	Unk4 = 0x10,                        //  4
	AbsorbEnvironmentalDamage = 0x20,   //  5
	Unk6 = 0x40,                        //  6
	RankIgnoresCasterLevel = 0x80,      //  7 Spell_C_GetSpellRank returns SpellLevels.MaxLevel * 5 instead of std::min(SpellLevels.MaxLevel, caster.Level) * 5
	Unk8 = 0x100,                       //  8
	Unk9 = 0x200,                       //  9
	Unk10 = 0x400,                      // 10
	NotUsableInInstances = 0x800,       // 11
	Unk12 = 0x1000,                     // 12
	Unk13 = 0x2000,                     // 13
	Unk14 = 0x4000,                     // 14
	Unk15 = 0x8000,                     // 15
	NotUsableInChallengeMode = 0x10000, // 16
	Unk17 = 0x20000,                    // 17
	Unk18 = 0x40000,                    // 18
	Unk19 = 0x80000,                    // 19
	Unk20 = 0x100000,                   // 20
	Unk21 = 0x200000,                   // 21
	Unk22 = 0x400000,                   // 22
	Unk23 = 0x800000,                   // 23
	Unk24 = 0x1000000,                  // 24
	Unk25 = 0x2000000,                  // 25
	Unk26 = 0x4000000,                  // 26
	Unk27 = 0x8000000,                  // 27
	Unk28 = 0x10000000,                 // 28
	Unk29 = 0x20000000,                 // 29
	Unk30 = 0x40000000,                 // 30
	Unk31 = 0x80000000                  // 31
}