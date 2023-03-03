using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class MostHPPercentMissingInRange : ICheck<Unit>
{
    readonly Unit _obj;
    readonly float _range;
    readonly float _minHpPct;
    readonly float _maxHpPct;
    float _hpPct;

    public MostHPPercentMissingInRange(Unit obj, float range, uint minHpPct, uint maxHpPct)
    {
        _obj = obj;
        _range = range;
        _minHpPct = minHpPct;
        _maxHpPct = maxHpPct;
        _hpPct = 101.0f;
    }

    public bool Invoke(Unit u)
    {
        if (u.IsAlive() && u.IsInCombat() && !_obj.IsHostileTo(u) && _obj.IsWithinDist(u, _range) && _minHpPct <= u.GetHealthPct() && u.GetHealthPct() <= _maxHpPct && u.GetHealthPct() < _hpPct)
        {
            _hpPct = u.GetHealthPct();
            return true;
        }
        return false;
    }
}