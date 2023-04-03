// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgRoleCheck
{
    public long CancelTime;
    public List<uint> Dungeons = new();
    public ObjectGuid Leader;
    public uint RDungeonId;
    public Dictionary<ObjectGuid, LfgRoles> Roles = new();
    public LfgRoleCheckState State;
}