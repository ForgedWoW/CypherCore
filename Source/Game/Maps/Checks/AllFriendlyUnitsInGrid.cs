using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class AllFriendlyUnitsInGrid : ICheck<Unit>
{
    public AllFriendlyUnitsInGrid(Unit obj)
    {
        unit = obj;
    }

    public bool Invoke(Unit u)
    {
        if (u.IsAlive() && u.IsVisible() && u.IsFriendlyTo(unit))
            return true;

        return false;
    }

    readonly Unit unit;
}