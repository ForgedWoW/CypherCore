// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using System.Linq;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(new uint[]
{
    MonkSpells.CHI_WAVE_DAMAGE, MonkSpells.CHI_WAVE_HEAL
})]
public class SpellMonkChiWaveTargetSelector : SpellScript, IHasSpellEffects
{
    private bool _mShouldHeal;
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override bool Load()
    {
        _mShouldHeal = true; // just for initializing

        return true;
    }

    public override void Register()
    {
        SpellEffects.Add(new ObjectAreaTargetSelectHandler(SelectTarget, 1, Targets.UnitDestAreaEntry));
        SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void SelectTarget(List<WorldObject> targets)
    {
        if (targets.Count == 0)
            return;

        var spellInfo = TriggeringSpell;

        if (spellInfo.Id == 132467) // Triggered by damage, so we need heal selector
        {
            targets.RemoveIf(new HealUnitCheck(Caster));
            targets.Sort(new HealthPctOrderPred(false)); // Reverse order due to target is selected via std::list back
            _mShouldHeal = true;
        }
        else if (spellInfo.Id == 132464) // Triggered by heal, so we need damage selector
        {
            targets.RemoveIf(new DamageUnitCheck(Caster, 25.0f));
            _mShouldHeal = false;
        }

        if (targets.Count == 0)
            return;

        var target = targets.LastOrDefault();

        if (target == null)
            return;

        targets.Clear();
        targets.Add(target);
    }

    private void HandleDummy(int effIndex)
    {
        if (EffectValue != 0) // Ran out of bounces
            return;

        if (!ExplTargetUnit || !OriginalCaster)
            return;

        var target = HitUnit;

        if (_mShouldHeal)
            ExplTargetUnit.SpellFactory.CastSpell(target, 132464, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, EffectValue).SetOriginalCaster(OriginalCaster.GUID));
        else
            ExplTargetUnit.SpellFactory.CastSpell(target, 132467, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, EffectValue).SetOriginalCaster(OriginalCaster.GUID));
    }
}