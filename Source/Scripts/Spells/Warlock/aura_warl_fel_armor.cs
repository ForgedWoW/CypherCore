// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.FEL_ARMOR_DMG_DELAY)]
internal class AuraWarlFelArmor : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectPeriodicHandler(PeriodicDamage, 0, Framework.Constants.AuraType.PeriodicDamage));
    }

    void PeriodicDamage(AuraEffect eff)
    {
        if (!TryGetCasterAsPlayer(out var player) || !player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING, out var felArmor))
            return;

        if (felArmor.TryGetEffect(1, out var remaining))
        {
            var rem = remaining.Amount - eff.Amount;

            if (rem <= 0)
            {
                player.RemoveAura(felArmor);
                player.RemoveAura(eff.Base);
            }
            else
            {
                remaining.SetAmount(rem);
            }
        }
    }
}