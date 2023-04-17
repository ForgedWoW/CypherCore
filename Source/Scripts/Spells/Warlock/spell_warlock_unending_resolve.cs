// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 104773 - Unending Resolve
[SpellScript(104773)]
internal class SpellWarlockUnendingResolve : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 0, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
        AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 3, AuraType.MechanicImmunity, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(PreventEffectIfCastingCircle, 3, AuraType.MechanicImmunity, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }

    private void PreventEffectIfCastingCircle(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        var caster = Caster;

        if (caster == null || caster.AsPlayer)
            return;

        var pCaster = caster.AsPlayer;

        if (pCaster == null)
            return;

        if (pCaster.HasSpell(WarlockSpells.CASTING_CIRCLE))
            PreventDefaultAction();
    }
}