// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Shaman;

//207498 ancestral protection
[SpellScript(207498)]
public class SpellShaAncestralProtectionTotemAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectApplyHandler(HandleAfterRemove, 1, AuraType.SchoolAbsorb, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        amount.Value = -1;
    }

    private void HandleAfterRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        if (TargetApplication.RemoveMode != AuraRemoveMode.Death)
            return;

        var totem = Caster;

        if (totem == null)
            return;

        totem.SpellFactory.CastSpell(TargetApplication.Target, TotemSpells.TOTEM_TOTEMIC_REVIVAL, true);
        totem.KillSelf();
    }
}