// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock;

[SpellScript(198590)] // 198590 - Drain Soul
internal class aura_warl_drain_soul : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleRemove, 0, AuraType.PeriodicDamage, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void HandleRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        if (TargetApplication.RemoveMode != AuraRemoveMode.Death)
            return;

        var caster = Caster;

        caster?.CastSpell(caster, WarlockSpells.DRAIN_SOUL_ENERGIZE, true);
    }
}