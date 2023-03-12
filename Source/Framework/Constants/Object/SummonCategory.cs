// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Framework.Constants;

public enum SummonCategory
{
	Wild = 0,
	Ally = 1,
	Pet = 2,
	Puppet = 3,
	Vehicle = 4,

	Unk = 5 // as of patch 3.3.5a only Bone Spike in Icecrown Citadel
	// uses this category
}