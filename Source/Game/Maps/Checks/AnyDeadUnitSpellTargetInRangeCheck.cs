using Framework.Constants;
using Game.Entities;
using Game.Spells;

namespace Game.Maps;

public class AnyDeadUnitSpellTargetInRangeCheck<T> : AnyDeadUnitObjectInRangeCheck<T> where T : WorldObject
{
    public AnyDeadUnitSpellTargetInRangeCheck(WorldObject searchObj, float range, SpellInfo spellInfo, SpellTargetCheckTypes check, SpellTargetObjectTypes objectType) : base(searchObj, range)
    {
        i_check = new WorldObjectSpellTargetCheck(searchObj, searchObj, spellInfo, check, null, objectType);
    }

    public override bool Invoke(T obj)
    {
        return base.Invoke(obj) && i_check.Invoke(obj);
    }

    readonly WorldObjectSpellTargetCheck i_check;
}