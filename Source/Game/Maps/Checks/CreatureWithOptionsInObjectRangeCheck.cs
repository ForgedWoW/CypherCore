using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;

namespace Game.Maps;

public class CreatureWithOptionsInObjectRangeCheck<T> : ICheck<Creature> where T : NoopCheckCustomizer
{
    readonly WorldObject i_obj;
    FindCreatureOptions i_args;
    readonly T i_customizer;

    public CreatureWithOptionsInObjectRangeCheck(WorldObject obj, T customizer, FindCreatureOptions args)
    {
        i_obj = obj;
        i_args = args;
        i_customizer = customizer;
    }

    public bool Invoke(Creature u)
    {
        if (u.GetDeathState() == DeathState.Dead) // Despawned
            return false;

        if (u.GetGUID() == i_obj.GetGUID())
            return false;

        if (!i_customizer.Test(u))
            return false;

        if (i_args.CreatureId.HasValue && u.GetEntry() != i_args.CreatureId)
            return false;

        if (i_args.StringId != null && !u.HasStringId(i_args.StringId))
            return false;

        if (i_args.IsAlive.HasValue && u.IsAlive() != i_args.IsAlive)
            return false;

        if (i_args.IsSummon.HasValue && u.IsSummon() != i_args.IsSummon)
            return false;

        if (i_args.IsInCombat.HasValue && u.IsInCombat() != i_args.IsInCombat)
            return false;

        if ((i_args.OwnerGuid.HasValue && u.GetOwnerGUID() != i_args.OwnerGuid)
            || (i_args.CharmerGuid.HasValue && u.GetCharmerGUID() != i_args.CharmerGuid)
            || (i_args.CreatorGuid.HasValue && u.GetCreatorGUID() != i_args.CreatorGuid)
            || (i_args.DemonCreatorGuid.HasValue && u.GetDemonCreatorGUID() != i_args.DemonCreatorGuid)
            || (i_args.PrivateObjectOwnerGuid.HasValue && u.GetPrivateObjectOwner() != i_args.PrivateObjectOwnerGuid))
            return false;

        if (i_args.IgnorePrivateObjects && u.IsPrivateObject())
            return false;

        if (i_args.IgnoreNotOwnedPrivateObjects && !u.CheckPrivateObjectOwnerVisibility(i_obj))
            return false;

        if (i_args.AuraSpellId.HasValue && !u.HasAura((uint)i_args.AuraSpellId))
            return false;

        i_customizer.Update(u);
        return true;
    }
}