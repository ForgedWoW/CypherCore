// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.DemonHunter;

[SpellScript(205411)]
public class spell_dh_desperate_instincts : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.TriggerSpellOnHealthPct, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        PreventDefaultAction();
        var caster = Caster;

        if (caster == null || eventInfo.DamageInfo != null)
            return;

        if (caster.SpellHistory.HasCooldown(DemonHunterSpells.BLUR_BUFF))
            return;

        var triggerOnHealth = caster.CountPctFromMaxHealth(aurEff.Amount);
        var currentHealth = caster.Health;

        // Just falling below threshold
        if (currentHealth > triggerOnHealth && (currentHealth - eventInfo.DamageInfo.Damage) <= triggerOnHealth)
            caster.CastSpell(caster, DemonHunterSpells.BLUR_BUFF, false);
    }
}