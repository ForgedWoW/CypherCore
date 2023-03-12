// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum InhabitType
{
	Ground = 1,
	Water = 2,
	Air = 4,
	Root = 8,
	Anywhere = Ground | Water | Air | Root
}