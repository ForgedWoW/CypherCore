// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Groups;

namespace Forged.MapServer.Scripting.Interfaces.IGroup;

public interface IGroupOnChangeLeader : IScriptObject
{
    void OnChangeLeader(PlayerGroup group, ObjectGuid newLeaderGuid, ObjectGuid oldLeaderGuid);
}