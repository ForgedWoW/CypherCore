// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(257538)]
public class SpellMageEbonboltDamage : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void DoEffectHitTarget(int effIndex)
    {
        var hitUnit = HitUnit;
        var primaryTarget = Caster.VariableStorage.GetValue<ObjectGuid>("explTarget", default);
        var damage = HitDamage;

        if (hitUnit == null || primaryTarget == default)
            return;

        var eff1 = Global.SpellMgr.GetSpellInfo(MageSpells.SPLITTING_ICE, Difficulty.None).GetEffect(1).CalcValue();

        if (eff1 != 0)
            if (hitUnit.GUID != primaryTarget)
                HitDamage = MathFunctions.CalculatePct(damage, eff1);
    }
}