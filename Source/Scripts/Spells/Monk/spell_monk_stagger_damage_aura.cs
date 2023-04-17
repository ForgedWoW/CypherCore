// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[Script] // 124255 - Stagger - STAGGER_DAMAGE_AURA
internal class SpellMonkStaggerDamageAura : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(OnPeriodicDamage, 0, AuraType.PeriodicDamage));
    }

    private void OnPeriodicDamage(AuraEffect aurEff)
    {
        // Update our light/medium/heavy stagger with the correct stagger amount left
        var auraStagger = SpellMonkStagger.FindExistingStaggerEffect(Target);

        if (auraStagger != null)
        {
            var auraEff = auraStagger.GetEffect(1);

            if (auraEff != null)
            {
                var total = auraEff.Amount;
                var tickDamage = aurEff.Amount;
                auraEff.ChangeAmount((int)(total - tickDamage));
            }
        }
    }
}