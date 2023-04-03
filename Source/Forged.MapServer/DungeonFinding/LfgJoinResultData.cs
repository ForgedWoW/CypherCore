// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.DungeonFinding;

public class LfgJoinResultData
{
    public Dictionary<ObjectGuid, Dictionary<uint, LfgLockInfoData>> Lockmap = new();
    public List<string> PlayersMissingRequirement = new();
    public LfgJoinResult Result;
    public LfgRoleCheckState State;

    public LfgJoinResultData(LfgJoinResult result = LfgJoinResult.Ok, LfgRoleCheckState state = LfgRoleCheckState.Default)
    {
        Result = result;
        State = state;
    }
}