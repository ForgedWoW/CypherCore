// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum MovementGeneratorFlags
{
	None = 0x000,
	InitializationPending = 0x001,
	Initialized = 0x002,
	SpeedUpdatePending = 0x004,
	Interrupted = 0x008,
	Paused = 0x010,
	TimedPaused = 0x020,
	Deactivated = 0x040,
	InformEnabled = 0x080,
	Finalized = 0x100,
	PersistOnDeath = 0x200,

	Transitory = SpeedUpdatePending | Interrupted
}