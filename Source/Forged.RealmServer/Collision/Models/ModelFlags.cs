// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Forged.RealmServer.Collision;

public enum ModelFlags
{
	M2 = 1,
	HasBound = 1 << 1,
	ParentSpawn = 1 << 2
}