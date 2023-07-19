// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.DemonHunter;

[SpellScript(201472)]
public class SpellDhArtifactRageOfTheIllidari : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(OnProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }


    private void OnProc(AuraEffect aurEff, ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (caster == null || eventInfo.DamageInfo != null)
            return;

        var damage = MathFunctions.CalculatePct(eventInfo.DamageInfo.Damage, aurEff.SpellEffectInfo.BasePoints);

        if (damage == 0)
            return;

        // damage += caster->VariableStorage.GetValue<int32>("Spells.RageOfTheIllidariDamage");

        //  caster->VariableStorage.Set("Spells.RageOfTheIllidariDamage", damage);
    }
}