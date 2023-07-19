// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[Script]
internal class SpellMageRayOfFrostAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(HandleEffectPeriodic, 1, AuraType.PeriodicDamage));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 1, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleEffectPeriodic(AuraEffect aurEff)
    {
        var caster = Caster;

        if (caster != null)
            if (aurEff.TickNumber > 1) // First tick should deal base Damage
                caster.SpellFactory.CastSpell(caster, MageSpells.RAY_OF_FROST_BONUS, true);
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        caster?.RemoveAura(MageSpells.RAY_OF_FROST_FINGERS_OF_FROST);
    }
}