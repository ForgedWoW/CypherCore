// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Hunter;

[SpellScript(199483)]
public class SpellHunCamouflage : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.ModStealth, AuraEffectHandleModes.Real));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.ModStealth, AuraEffectHandleModes.Real, AuraScriptHookType.EffectRemove));
    }


    private void OnApply(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        if (Caster && Caster.IsPlayer)
        {
            Unit pet = Caster.GetGuardianPet();

            if (pet != null)
                pet.SpellFactory.CastSpell(pet, HunterSpells.CAMOUFLAGE, true);
        }
    }

    private void OnRemove(AuraEffect unnamedParameter, AuraEffectHandleModes unnamedParameter2)
    {
        if (Caster && Caster.IsPlayer)
        {
            Unit pet = Caster.GetGuardianPet();

            if (pet != null)
                pet.RemoveAura(HunterSpells.CAMOUFLAGE);
        }
    }
}