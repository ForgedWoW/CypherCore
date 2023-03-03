using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class PlayerAtMinimumRangeAway : ICheck<Player>
{
    public PlayerAtMinimumRangeAway(Unit _unit, float fMinRange)
    {
        unit = _unit;
        fRange = fMinRange;
    }

    public bool Invoke(Player player)
    {
        //No threat list check, must be done explicit if expected to be in combat with creature
        if (!player.IsGameMaster() && player.IsAlive() && !unit.IsWithinDist(player, fRange, false))
            return true;

        return false;
    }

    Unit unit;
    float fRange;
}