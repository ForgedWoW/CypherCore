// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// Enrage Aura - 184362
[SpellScript(184362)]
public class SpellWarrEnrageAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.MeleeSlow, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster != null)
            if (caster.HasAura(WarriorSpells.ENDLESS_RAGE))
                caster.SpellFactory.CastSpell(null, WarriorSpells.ENDLESS_RAGE_GIVE_POWER, true);
    }

    private void HandleRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;
        caster.RemoveAura(WarriorSpells.UNCHACKLED_FURY);
    }
}