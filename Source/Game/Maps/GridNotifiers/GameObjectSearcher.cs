using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class GameObjectSearcher : IGridNotifierGameObject
{
    PhaseShift i_phaseShift;
    GameObject i_object;
    ICheck<GameObject> i_check;

    public GridType GridType { get; set; }

    public GameObjectSearcher(WorldObject searcher, ICheck<GameObject> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<GameObject> objs)
    {
        // already found
        if (i_object)
            return;

        for (var i = 0; i < objs.Count; ++i)
        {
            GameObject gameObject = objs[i];
            if (!gameObject.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(gameObject))
            {
                i_object = gameObject;
                return;
            }
        }
    }

    public GameObject GetTarget() { return i_object; }
}