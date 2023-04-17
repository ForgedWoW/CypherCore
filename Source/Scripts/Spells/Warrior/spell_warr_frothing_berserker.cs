// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

//215571 Frothing Berserker
[SpellScript(215571)]
public class SpellWarrFrothingBerserker : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.ProcTriggerSpell, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 1, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 2, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 3, AuraType.AddFlatModifier, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo unnamedParameter2)
    {
        Caster.SpellFactory.CastSpell(Caster, WarriorSpells.FROTHING_BERSERKER, true);
    }
}