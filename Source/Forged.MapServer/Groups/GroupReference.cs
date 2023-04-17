// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Dynamic;

namespace Forged.MapServer.Groups;

public class GroupReference : Reference<PlayerGroup, Player>
{
    public GroupReference()
    {
        SubGroup = 0;
    }

    public byte SubGroup { get; set; }

    public new GroupReference Next()
    {
        return (GroupReference)base.Next();
    }

    public override void TargetObjectBuildLink()
    {
        Target.LinkMember(this);
    }

    ~GroupReference()
    {
        Unlink();
    }
}