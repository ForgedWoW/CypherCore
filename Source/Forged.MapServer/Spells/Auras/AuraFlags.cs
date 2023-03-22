// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Game.Spells;

[Flags]
public enum AuraFlags
{
	None = 0x00,
	NoCaster = 0x01,
	Positive = 0x02,
	Duration = 0x04,
	Scalable = 0x08,
	Negative = 0x10,
	Unk20 = 0x20,
	Unk40 = 0x40,
	Unk80 = 0x80,
	MawPower = 0x100
}