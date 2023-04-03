// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgPlayerBoot
{
    public long CancelTime;
    public bool InProgress;
    public string Reason;
    public ObjectGuid Victim;
    public Dictionary<ObjectGuid, LfgAnswer> Votes = new();
}