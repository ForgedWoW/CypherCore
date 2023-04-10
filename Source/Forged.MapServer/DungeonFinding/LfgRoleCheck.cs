// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgRoleCheck
{
    public long CancelTime { get; set; }
    public List<uint> Dungeons { get; set; } = new();
    public ObjectGuid Leader { get; set; }
    public uint RDungeonId { get; set; }
    public Dictionary<ObjectGuid, LfgRoles> Roles { get; set; } = new();
    public LfgRoleCheckState State { get; set; }
}