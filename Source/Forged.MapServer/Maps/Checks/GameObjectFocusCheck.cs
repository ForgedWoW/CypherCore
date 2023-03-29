// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.GameObjects;
using Forged.MapServer.Entities.Objects;

namespace Forged.MapServer.Maps.Checks;

internal class GameObjectFocusCheck : ICheck<GameObject>
{
    private readonly WorldObject _caster;
    private readonly uint _focusId;

    public GameObjectFocusCheck(WorldObject caster, uint focusId)
    {
        _caster = caster;
        _focusId = focusId;
    }

    public bool Invoke(GameObject go)
    {
        if (go.Template.GetSpellFocusType() != _focusId)
            return false;

        if (!go.IsSpawned)
            return false;

        float dist = go.Template.GetSpellFocusRadius();

        return go.Location.IsWithinDist(_caster, dist);
    }
}