// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum PlayerCurrencyState
{
	Unchanged = 0,
	Changed = 1,
	New = 2,
	Removed = 3 //not removed just set count == 0
}