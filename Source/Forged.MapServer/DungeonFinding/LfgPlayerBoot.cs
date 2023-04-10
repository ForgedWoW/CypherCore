// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgPlayerBoot
{
    public long CancelTime { get; set; }
    public bool InProgress { get; set; }
    public string Reason { get; set; }
    public ObjectGuid Victim { get; set; }
    public Dictionary<ObjectGuid, LfgAnswer> Votes { get; set; } = new();
}