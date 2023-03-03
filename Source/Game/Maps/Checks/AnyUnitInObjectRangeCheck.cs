using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class AnyUnitInObjectRangeCheck : ICheck<Unit>
{
    public AnyUnitInObjectRangeCheck(WorldObject obj, float range, bool check3D = true)
    {
        i_obj = obj;
        i_range = range;
        i_check3D = check3D;
    }

    public bool Invoke(Unit u)
    {
        if (u.IsAlive() && i_obj.IsWithinDist(u, i_range, i_check3D))
            return true;

        return false;
    }

    readonly WorldObject i_obj;
    readonly float i_range;
    readonly bool i_check3D;
}