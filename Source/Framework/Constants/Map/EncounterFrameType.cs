// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum EncounterFrameType
{
	SetCombatResLimit = 0,
	ResetCombatResLimit = 1,
	Engage = 2,
	Disengage = 3,
	UpdatePriority = 4,
	AddTimer = 5,
	EnableObjective = 6,
	UpdateObjective = 7,
	DisableObjective = 8,
	Unk7 = 9, // Seems To Have Something To Do With Sorting The Encounter Units
	AddCombatResLimit = 10
}