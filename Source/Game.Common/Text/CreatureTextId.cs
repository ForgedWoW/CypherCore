using Game;
// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

namespace Game.Common.Text;

public class CreatureTextId
{
	public uint entry;
	public uint textGroup;
	public uint textId;

	public CreatureTextId(uint e, uint g, uint i)
	{
		entry = e;
		textGroup = g;
		textId = i;
	}
}
