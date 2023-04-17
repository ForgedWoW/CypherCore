// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(257537)]
public class SpellMageEbonbolt : SpellScript, IHasSpellEffects, ISpellOnCast
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void OnCast()
    {
        Caster.SpellFactory.CastSpell(Caster, MageSpells.BRAIN_FREEZE_AURA, true);
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

        Caster.SpellFactory.CastSpell(hitUnit, MageSpells.EBONBOLT_DAMAGE, true);
    }
}