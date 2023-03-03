using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Maps.Interfaces;

namespace Game.Maps;

public class UnitSearcher : IGridNotifierPlayer, IGridNotifierCreature
{
    PhaseShift i_phaseShift;
    Unit i_object;
    ICheck<Unit> i_check;
    public GridType GridType { get; set; }

    public UnitSearcher(WorldObject searcher, ICheck<Unit> check, GridType gridType)
    {
        i_phaseShift = searcher.GetPhaseShift();
        i_check = check;
        GridType = gridType;
    }

    public void Visit(IList<Player> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Player player = objs[i];
            if (!player.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(player))
            {
                i_object = player;
                return;
            }
        }
    }

    public void Visit(IList<Creature> objs)
    {
        for (var i = 0; i < objs.Count; ++i)
        {
            Creature creature = objs[i];
            if (!creature.InSamePhase(i_phaseShift))
                continue;

            if (i_check.Invoke(creature))
            {
                i_object = creature;
                return;
            }
        }
    }

    public Unit GetTarget() { return i_object; }
}