using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class AnyDeadUnitObjectInRangeCheck<T> : ICheck<T> where T : WorldObject
{
    public AnyDeadUnitObjectInRangeCheck(WorldObject searchObj, float range)
    {
        i_searchObj = searchObj;
        i_range = range;
    }

    public virtual bool Invoke(T obj)
    {
        Player player = obj.ToPlayer();
        if (player)
            return !player.IsAlive() && !player.HasAuraType(AuraType.Ghost) && i_searchObj.IsWithinDistInMap(player, i_range);

        Creature creature = obj.ToCreature();
        if (creature)
            return !creature.IsAlive() && i_searchObj.IsWithinDistInMap(creature, i_range);

        Corpse corpse = obj.ToCorpse();
        if (corpse)
            return corpse.GetCorpseType() != CorpseType.Bones && i_searchObj.IsWithinDistInMap(corpse, i_range);

        return false;
    }

    readonly WorldObject i_searchObj;
    readonly float i_range;
}