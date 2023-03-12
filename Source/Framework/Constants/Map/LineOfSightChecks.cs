// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum LineOfSightChecks
{
	Vmap = 0x1,    // check static floor layout data
	Gobject = 0x2, // check dynamic game object data

	All = Vmap | Gobject
}