// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;

namespace Framework.Constants;

[Flags]
public enum GarrisonAbilityFlags : int
{
	Trait = 0x01,
	CannotRoll = 0x02,
	HordeOnly = 0x04,
	AllianceOnly = 0x08,
	CannotRemove = 0x10,
	Exclusive = 0x20,
	SingleMissionDuration = 0x40,
	ActiveOnlyOnZoneSupport = 0x80,
	ApplyToFirstMission = 0x100,
	IsSpecialization = 0x200,
	IsEmptySlot = 0x400
}