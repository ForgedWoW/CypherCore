using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Maps;
using Framework.Constants;

namespace Forged.MapServer.Conditions;

public class ConditionSourceInfo
{
    public Map mConditionMap;
    public WorldObject[] mConditionTargets = new WorldObject[SharedConst.MaxConditionTargets]; // an array of targets available for conditions
    public Condition mLastFailedCondition;

    public ConditionSourceInfo(WorldObject target0, WorldObject target1 = null, WorldObject target2 = null)
    {
        mConditionTargets[0] = target0;
        mConditionTargets[1] = target1;
        mConditionTargets[2] = target2;
        mConditionMap = target0?.Location.Map;
        mLastFailedCondition = null;
    }

    public ConditionSourceInfo(Map map)
    {
        mConditionMap = map;
        mLastFailedCondition = null;
    }
}