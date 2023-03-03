using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UpdaterNotifier : IGridNotifierWorldObject
{
    public GridType GridType { get; set; }
    public UpdaterNotifier(uint diff, GridType gridType)
    {
        i_timeDiff = diff;
        GridType = gridType;
    }

    public void Visit(IList<WorldObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            WorldObject obj = objs[i];

            if (obj.IsTypeId(TypeId.Player) || obj.IsTypeId(TypeId.Corpse))
                continue;

            if (obj.IsInWorld)
                obj.Update(i_timeDiff);
        }
    }

    readonly uint i_timeDiff;
}