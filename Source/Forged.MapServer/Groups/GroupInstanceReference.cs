// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Framework.Dynamic;

namespace Forged.MapServer.Groups;

public class GroupInstanceReference : Reference<PlayerGroup, InstanceMap>
{
    ~GroupInstanceReference()
    {
        Unlink();
    }

    public new GroupInstanceReference Next()
    {
        return (GroupInstanceReference)base.Next();
    }

    public override void TargetObjectBuildLink()
    {
        Target.LinkOwnedInstance(this);
    }
}