// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Maps;
using Game.Common.Groups;

namespace Game.Common.Groups;

class GroupInstanceRefManager : RefManager<PlayerGroup, InstanceMap>
{
	public new GroupInstanceReference GetFirst()
	{
		return (GroupInstanceReference)base.GetFirst();
	}
}
