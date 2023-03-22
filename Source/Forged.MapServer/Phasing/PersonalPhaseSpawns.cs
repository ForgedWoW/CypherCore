// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Game.Entities;

namespace Game;

class PersonalPhaseSpawns
{
	public static TimeSpan DELETE_TIME_DEFAULT = TimeSpan.FromMinutes(1);

	public List<WorldObject> Objects = new();
	public List<ushort> Grids = new();
	public TimeSpan? DurationRemaining;

	public bool IsEmpty()
	{
		return Objects.Empty() && Grids.Empty();
	}
}