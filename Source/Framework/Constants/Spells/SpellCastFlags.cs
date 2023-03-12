// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum SpellCastFlags : uint
{
	None = 0x0,
	Pending = 0x01, // Aoe Combat Log?
	HasTrajectory = 0x02,
	Unk3 = 0x04,
	Unk4 = 0x08, // Ignore Aoe Visual
	Unk5 = 0x10,
	Projectile = 0x20,
	Unk7 = 0x40,
	Unk8 = 0x80,
	Unk9 = 0x100,
	Unk10 = 0x200,
	Unk11 = 0x400,
	PowerLeftSelf = 0x800,
	Unk13 = 0x1000,
	Unk14 = 0x2000,
	Unk15 = 0x4000,
	Unk16 = 0x8000,
	Unk17 = 0x10000,
	AdjustMissile = 0x20000,
	NoGCD = 0x40000,
	VisualChain = 0x80000,
	Unk21 = 0x100000,
	RuneList = 0x200000,
	Unk23 = 0x400000,
	Unk24 = 0x800000,
	Unk25 = 0x1000000,
	Unk26 = 0x2000000,
	Immunity = 0x4000000,
	Unk28 = 0x8000000,
	Unk29 = 0x10000000,
	Unk30 = 0x20000000,
	HealPrediction = 0x40000000,
	Unk32 = 0x80000000
}