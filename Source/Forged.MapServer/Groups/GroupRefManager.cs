// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Dynamic;

namespace Forged.MapServer.Groups;

public class GroupRefManager : RefManager<PlayerGroup, Player>
{
	public new GroupReference GetFirst()
	{
		return (GroupReference)base.GetFirst();
	}
}