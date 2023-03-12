// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum UnitTypeMask
{
	None = 0x0,
	Summon = 0x01,
	Minion = 0x02,
	Guardian = 0x04,
	Totem = 0x08,
	Pet = 0x10,
	Vehicle = 0x20,
	Puppet = 0x40,
	HunterPet = 0x80,
	ControlableGuardian = 0x100,
	Accessory = 0x200
}