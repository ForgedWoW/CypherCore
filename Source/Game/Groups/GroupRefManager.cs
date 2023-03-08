// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Entities;

namespace Game.Groups;

public class GroupRefManager : RefManager<PlayerGroup, Player>
{
	public new GroupReference GetFirst()
	{
		return (GroupReference)base.GetFirst();
	}
}