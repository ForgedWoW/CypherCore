// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(185244)]
public class SpellDemonHunterPain : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.ModPowerDisplay, AuraScriptHookType.EffectProc));
    }

    private void OnProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null || eventInfo.DamageInfo != null)
            return;

        if (eventInfo.SpellInfo != null && eventInfo.SpellInfo.IsPositive)
            return;

        var damageTaken = eventInfo.DamageInfo.Damage;

        if (damageTaken <= 0)
            return;

        var painAmount = (50.0f * (double)damageTaken) / (double)caster.MaxHealth;
        caster.SpellFactory.CastSpell(caster, DemonHunterSpells.REWARD_PAIN, (int)painAmount);
    }
}