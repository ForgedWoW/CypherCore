﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Entities;

public class ItemPosCount
{
	public ushort Pos;
	public uint Count;

	public ItemPosCount(ushort pos, uint count)
	{
		Pos = pos;
		Count = count;
	}

	public bool IsContainedIn(List<ItemPosCount> vec)
	{
		foreach (var posCount in vec)
			if (posCount.Pos == Pos)
				return true;

		return false;
	}
}