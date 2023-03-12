// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SpellState
{
	None = 0,
	Preparing = 1,
	Casting = 2,
	Finished = 3,
	Idle = 4,
	Delayed = 5
}