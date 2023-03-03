using System.Collections.Generic;
using Game.Entities;

namespace Game.Maps;

class GameObjectFocusCheck : ICheck<GameObject>
{
    public GameObjectFocusCheck(WorldObject caster, uint focusId)
    {
        _caster = caster;
        _focusId = focusId;
    }

    public bool Invoke(GameObject go)
    {
        if (go.GetGoInfo().GetSpellFocusType() != _focusId)
            return false;

        if (!go.IsSpawned())
            return false;

        float dist = go.GetGoInfo().GetSpellFocusRadius();
        return go.IsWithinDist(_caster, dist);
    }

    readonly WorldObject _caster;
    readonly uint _focusId;
}