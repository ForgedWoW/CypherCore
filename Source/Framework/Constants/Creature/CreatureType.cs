// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum CreatureType
{
	Beast = 1,
	Dragonkin = 2,
	Demon = 3,
	Elemental = 4,
	Giant = 5,
	Undead = 6,
	Humanoid = 7,
	Critter = 8,
	Mechanical = 9,
	NotSpecified = 10,
	Totem = 11,
	NonCombatPet = 12,
	GasCloud = 13,
	WildPet = 14,
	Aberration = 15,

	MaskDemonOrUndead = (1 << (Demon - 1)) | (1 << (Undead - 1)),
	MaskHumanoidOrUndead = (1 << (Humanoid - 1)) | (1 << (Undead - 1)),
	MaskMechanicalOrElemental = (1 << (Mechanical - 1)) | (1 << (Elemental - 1))
}