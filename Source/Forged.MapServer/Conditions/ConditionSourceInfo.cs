// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Conditions;

public class ConditionSourceInfo
{
    public Map ConditionMap { get; set; }
    public WorldObject[] ConditionTargets { get; set; } = new WorldObject[SharedConst.MaxConditionTargets]; // an array of targets available for conditions
    public Condition LastFailedCondition { get; set; }

    public ConditionSourceInfo(WorldObject target0, WorldObject target1 = null, WorldObject target2 = null)
    {
        ConditionTargets[0] = target0;
        ConditionTargets[1] = target1;
        ConditionTargets[2] = target2;
        ConditionMap = target0?.Location.Map;
        LastFailedCondition = null;
    }

    public ConditionSourceInfo(Map map)
    {
        ConditionMap = map;
        LastFailedCondition = null;
    }
}