using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class GameObjectWorker : IGridNotifierGameObject
{
    PhaseShift i_phaseShift;
    IDoWork<GameObject> _do;
    public GridType GridType { get; set; }

    public GameObjectWorker(WorldObject searcher, IDoWork<GameObject> @do, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        _do = @do;
        GridType = gridType;
    }

    public void Visit(IList<GameObject> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            GameObject gameObject = objs[i];
            if (gameObject.InSamePhase(i_phaseShift))
                _do.Invoke(gameObject);
        }
    }
}