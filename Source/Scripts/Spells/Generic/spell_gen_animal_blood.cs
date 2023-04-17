// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script] // 46221 - Animal Blood
internal class SpellGenAnimalBlood : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectApplyHandler(OnApply, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterApply));
        AuraEffects.Add(new AuraEffectApplyHandler(OnRemove, 0, AuraType.PeriodicTriggerSpell, AuraEffectHandleModes.Real, AuraScriptHookType.EffectAfterRemove));
    }

    private void OnApply(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        // Remove all Auras with spell Id 46221, except the one currently being applied
        Aura aur;

        while ((aur = OwnerAsUnit.GetOwnedAura(GenericSpellIds.ANIMAL_BLOOD, ObjectGuid.Empty, ObjectGuid.Empty, Aura)) != null)
            OwnerAsUnit.RemoveOwnedAura(aur);
    }

    private void OnRemove(AuraEffect aurEff, AuraEffectHandleModes mode)
    {
        var owner = OwnerAsUnit;

        if (owner)
            owner.SpellFactory.CastSpell(owner, GenericSpellIds.SPAWN_BLOOD_POOL, true);
    }
}