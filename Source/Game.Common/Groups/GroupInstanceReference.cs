// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Maps;
using Game.Common.Groups;

namespace Game.Common.Groups;

public class GroupInstanceReference : Reference<PlayerGroup, InstanceMap>
{
	public new GroupInstanceReference Next()
	{
		return (GroupInstanceReference)base.Next();
	}

	public override void TargetObjectBuildLink()
	{
		Target.LinkOwnedInstance(this);
	}

	~GroupInstanceReference()
	{
		Unlink();
	}
}
