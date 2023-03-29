// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Monk;

[SpellScript(132464)]
public class spell_monk_chi_wave_heal_missile : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.Dummy, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes UnnamedParameter)
    {
        var caster = Caster;
        var target = Target;

        if (target == null || caster == null)
            return;

        caster.CastSpell(target, 132463, true);
        // rerun target selector
        caster.CastSpell(target, 132466, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint1, aurEff.Amount - 1).SetTriggeringAura(aurEff));
    }
}