// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Globals;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(new uint[]
{
    120761, 121414
})]
public class SpellHunGlaiveTossDamage : SpellScript, IHasSpellEffects, ISpellOnHit
{
    private ObjectGuid _mainTargetGUID = ObjectGuid.Empty;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public void OnHit()
    {
        if (_mainTargetGUID == default)
            return;

        var target = ObjectAccessor.Instance.GetUnit(Caster, _mainTargetGUID);

        if (target == null)
            return;

        if (HitUnit)
            if (HitUnit == target)
                HitDamage = HitDamage * 4;
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(CorrectDamageRange, 0, Targets.UnitDestAreaEnemy));
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(CorrectSnareRange, 1, Targets.UnitDestAreaEnemy));
    }

    private void CorrectDamageRange(List<WorldObject> targets)
    {
        targets.Clear();

        var targetList = new List<Unit>();
        var radius = 50.0f;

        Caster.GetAnyUnitListInRange(targetList, radius);

        foreach (var itr in targetList)
            if (itr.HasAura(HunterSpells.GLAIVE_TOSS_AURA))
            {
                _mainTargetGUID = itr.GUID;

                break;
            }

        if (_mainTargetGUID == default)
            return;

        var target = ObjectAccessor.Instance.GetUnit(Caster, _mainTargetGUID);

        if (target == null)
            return;

        targets.Add(target);

        foreach (var itr in targetList)
            if (itr.IsInBetween(Caster, target, 5.0f))
                if (!Caster.IsFriendlyTo(itr))
                    targets.Add(itr);
    }

    private void CorrectSnareRange(List<WorldObject> targets)
    {
        targets.Clear();

        var targetList = new List<Unit>();
        var radius = 50.0f;

        Caster.GetAnyUnitListInRange(targetList, radius);

        foreach (var itr in targetList)
            if (itr.HasAura(HunterSpells.GLAIVE_TOSS_AURA))
            {
                _mainTargetGUID = itr.GUID;

                break;
            }

        if (_mainTargetGUID == default)
            return;

        if (_mainTargetGUID == default)
            return;

        var target = ObjectAccessor.Instance.GetUnit(Caster, _mainTargetGUID);

        if (target == null)
            return;

        targets.Add(target);

        foreach (var itr in targetList)
            if (itr.IsInBetween(Caster, target, 5.0f))
                if (!Caster.IsFriendlyTo(itr))
                    targets.Add(itr);
    }
}