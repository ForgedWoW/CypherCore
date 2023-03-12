// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PhaseEntryFlags : ushort
{
	ReadOnly = 0x1,
	InternalPhase = 0x2,
	Normal = 0x8,
	Cosmetic = 0x010,
	Personal = 0x020,
	Expensive = 0x040,
	EventsAreObservable = 0x080,
	UsesPreloadConditions = 0x100,
	UnshareablePersonal = 0x200,
	ObjectsAreVisible = 0x400,
}