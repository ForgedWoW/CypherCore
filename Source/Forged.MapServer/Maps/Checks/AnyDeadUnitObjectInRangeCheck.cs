// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Framework.Constants;

namespace Forged.MapServer.Maps.Checks;

public class AnyDeadUnitObjectInRangeCheck<T> : ICheck<T> where T : WorldObject
{
    private readonly float _range;
    private readonly WorldObject _searchObj;
    public AnyDeadUnitObjectInRangeCheck(WorldObject searchObj, float range)
    {
        _searchObj = searchObj;
        _range = range;
    }

    public virtual bool Invoke(T obj)
    {
        var player = obj.AsPlayer;

        if (player != null)
            return !player.IsAlive && !player.HasAuraType(AuraType.Ghost) && _searchObj.Location.IsWithinDistInMap(player, _range);

        var creature = obj.AsCreature;

        if (creature != null)
            return !creature.IsAlive && _searchObj.Location.IsWithinDistInMap(creature, _range);

        var corpse = obj.AsCorpse;

        if (corpse != null)
            return corpse.GetCorpseType() != CorpseType.Bones && _searchObj.Location.IsWithinDistInMap(corpse, _range);

        return false;
    }
}