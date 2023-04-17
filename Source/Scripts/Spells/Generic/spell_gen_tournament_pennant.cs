// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenTournamentPennant : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override bool Load()
    {
        return Caster && Caster.IsTypeId(TypeId.Player);
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(HandleApplyEffect, 0, AuraType.Dummy, AuraEffectHandleModes.RealOrReapplyMask, AuraScriptHookType.EffectApply));
    }

    private void HandleApplyEffect(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var caster = Caster;

        if (caster)
            if (!caster.VehicleBase)
                caster.RemoveAura(Id);
    }
}