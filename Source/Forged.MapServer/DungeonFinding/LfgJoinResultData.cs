// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgJoinResultData
{
    public LfgJoinResultData(LfgJoinResult result = LfgJoinResult.Ok, LfgRoleCheckState state = LfgRoleCheckState.Default)
    {
        Result = result;
        State = state;
    }

    public Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> Lockmap { get; set; } = new();
    public List<string> PlayersMissingRequirement { get; set; } = new();
    public LfgJoinResult Result { get; set; }
    public LfgRoleCheckState State { get; set; }
}