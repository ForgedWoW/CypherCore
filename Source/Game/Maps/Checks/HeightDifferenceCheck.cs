using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

public class HeightDifferenceCheck : ICheck<WorldObject>
{
    public HeightDifferenceCheck(WorldObject go, float diff, bool reverse)
    {
        _baseObject = go;
        _difference = diff;
        _reverse = reverse;

    }

    public bool Invoke(WorldObject unit)
    {
        return (unit.GetPositionZ() - _baseObject.GetPositionZ() > _difference) != _reverse;
    }

    WorldObject _baseObject;
    float _difference;
    bool _reverse;
}