// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Dynamic;
using Game.Entities;

namespace Forged.RealmServer.Groups;

public class GroupReference : Reference<PlayerGroup, Player>
{
	byte _iSubGroup;

	public byte SubGroup
	{
		get => _iSubGroup;
		set => _iSubGroup = value;
	}

	public GroupReference()
	{
		_iSubGroup = 0;
	}

	public override void TargetObjectBuildLink()
	{
		Target.LinkMember(this);
	}

	public new GroupReference Next()
	{
		return (GroupReference)base.Next();
	}

	~GroupReference()
	{
		Unlink();
	}
}