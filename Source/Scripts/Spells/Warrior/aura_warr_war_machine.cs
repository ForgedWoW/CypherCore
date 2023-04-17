// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// War Machine 215556
[SpellScript(215556)]
public class AuraWarrWarMachine : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ProcTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            caster.SpellFactory.CastSpell(caster, WarriorSpells.WAR_MACHINE_AURA, true);
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            caster.RemoveAura(WarriorSpells.WAR_MACHINE_AURA);
    }
}