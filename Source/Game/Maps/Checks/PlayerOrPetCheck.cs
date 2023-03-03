using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class PlayerOrPetCheck : ICheck<WorldObject>
{
    public bool Invoke(WorldObject obj)
    {
        if (obj.IsTypeId(TypeId.Player))
            return false;

        Creature creature = obj.ToCreature();
        if (creature)
            return !creature.IsPet();

        return true;
    }
}