using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class PlayerDistWorker : IGridNotifierPlayer
{
    public GridType GridType { get; set; }
    WorldObject i_searcher;
    float i_dist;
    IDoWork<Player> _do;

    public PlayerDistWorker(WorldObject searcher, float _dist, IDoWork<Player> @do, GridType gridType)
    {
        i_searcher = searcher;
        i_dist = _dist;
        _do = @do;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (player.InSamePhase(i_searcher) && player.IsWithinDist(i_searcher, i_dist))
                _do.Invoke(player);
        }
    }
}