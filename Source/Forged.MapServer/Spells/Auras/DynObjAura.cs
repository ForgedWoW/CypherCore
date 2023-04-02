// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Maps;
using Forged.MapServer.Maps.GridNotifiers;
using Framework.Constants;

namespace Forged.MapServer.Spells.Auras;

public class DynObjAura : Aura
{
    public DynObjAura(AuraCreateInfo createInfo) : base(createInfo)
    {
        LoadScripts();
        _InitEffects(createInfo.AuraEffectMask, createInfo.Caster, createInfo.BaseAmount);
        DynobjOwner.SetAura(this);
    }

    public override Dictionary<Unit, HashSet<int>> FillTargetMap(Unit caster)
    {
        var targets = new Dictionary<Unit, HashSet<int>>();
        var dynObjOwnerCaster = DynobjOwner.GetCaster();
        var radius = DynobjOwner.GetRadius();

        foreach (var spellEffectInfo in SpellInfo.Effects)
        {
            if (!HasEffect(spellEffectInfo.EffectIndex))
                continue;

            // we can't use effect type like area auras to determine check type, check targets
            var selectionType = spellEffectInfo.TargetA.CheckType;

            if (spellEffectInfo.TargetB.ReferenceType == SpellTargetReferenceTypes.Dest)
                selectionType = spellEffectInfo.TargetB.CheckType;

            List<Unit> targetList = new();
            var condList = spellEffectInfo.ImplicitTargetConditions;

            WorldObjectSpellAreaTargetCheck check = new(radius, DynobjOwner.Location, dynObjOwnerCaster, dynObjOwnerCaster, SpellInfo, selectionType, condList, SpellTargetObjectTypes.Unit);
            UnitListSearcher searcher = new(DynobjOwner, targetList, check, GridType.All);
            Cell.VisitGrid(DynobjOwner, searcher, radius);

            // by design WorldObjectSpellAreaTargetCheck allows not-in-world units (for spells) but for auras it is not acceptable
            targetList.RemoveAll(unit => !unit.Location.IsSelfOrInSameMap(DynobjOwner));

            foreach (var unit in targetList)
            {
                if (!targets.ContainsKey(unit))
                    targets[unit] = new HashSet<int>();

                targets[unit].Add(spellEffectInfo.EffectIndex);
            }
        }

        return targets;
    }

    public override void Remove(AuraRemoveMode removeMode = AuraRemoveMode.Default)
    {
        if (IsRemoved)
            return;

        _Remove(removeMode);
        base.Remove(removeMode);
    }
}