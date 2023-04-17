// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.CHRONO_LOOP)]
public class AuraEvokerChronoLoop : AuraScript, IAuraOnApply, IAuraOnRemove
{
    long _health = 0;
    uint _mapId = 0;
    Position _pos;

    public void AuraApply()
    {
        var unit = OwnerAsUnit;
        _health = unit.Health;
        _mapId = unit.Location.MapId;
        _pos = new Position(unit.Location);
    }

    public void AuraRemoved(AuraRemoveMode removeMode)
    {
        var unit = OwnerAsUnit;

        if (!unit.IsAlive)
            return;

        unit.SetHealth(Math.Min(_health, unit.MaxHealth));

        if (unit.Location.MapId == _mapId)
            unit.UpdatePosition(_pos, true);
    }
}