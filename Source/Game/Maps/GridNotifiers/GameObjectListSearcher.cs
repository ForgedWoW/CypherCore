using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class GameObjectListSearcher : IGridNotifierGameObject
{
    readonly PhaseShift i_phaseShift;
    readonly List<GameObject> i_objects;
    readonly ICheck<GameObject> i_check;
    public GridType GridType { get; set; }

    public GameObjectListSearcher(WorldObject searcher, List<GameObject> objects, ICheck<GameObject> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_objects = objects;
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<GameObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            GameObject gameObject = objs[i];
            if (gameObject.InSamePhase(i_phaseShift))
                if (i_check.Invoke(gameObject))
                    i_objects.Add(gameObject);
        }
    }
}