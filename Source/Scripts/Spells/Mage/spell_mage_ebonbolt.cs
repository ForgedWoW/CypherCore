// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Mage;

[SpellScript(257537)]
public class spell_mage_ebonbolt : SpellScript, IHasSpellEffects, ISpellOnCast
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void OnCast()
    {
        Caster.CastSpell(Caster, MageSpells.BRAIN_FREEZE_AURA, true);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(DoEffectHitTarget, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void DoEffectHitTarget(int effIndex)
    {
        var explTarget = ExplTargetUnit;
        var hitUnit = HitUnit;

        if (hitUnit == null || explTarget == null)
            return;

        if (Caster.HasAura(MageSpells.SPLITTING_ICE))
            Caster.VariableStorage.Set<ObjectGuid>("explTarget", explTarget.GUID);

        Caster.CastSpell(hitUnit, MageSpells.EBONBOLT_DAMAGE, true);
    }
}