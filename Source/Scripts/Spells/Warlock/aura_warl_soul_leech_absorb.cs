// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(WarlockSpells.SOUL_LEECH_ABSORB)]
internal class AuraWarlSoulLeechAbsorb : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
    }

    private double OnAbsorb(AuraEffect effect, DamageInfo dmgInfo, double amount)
    {
        if (TryGetCasterAsPlayer(out var player) && player.TryGetAura(WarlockSpells.FEL_ARMOR, out var felArmor))
        {
            var damageToDistribute = MathFunctions.CalculatePct(amount, felArmor.GetEffect(0).Amount);

            if (player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING, out var dmgDelayRemaining))
            {
                damageToDistribute += dmgDelayRemaining.GetEffect(0).Amount;
                dmgDelayRemaining.GetEffect(0).SetAmount(damageToDistribute);
                dmgDelayRemaining.GetEffect(1).SetAmount(damageToDistribute);
            }
            else
            {
                player.SpellFactory.CastSpell(player,
                                 WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING,
                                 new CastSpellExtraArgs(true)
                                     .SetSpellValueMod(SpellValueMod.BasePoint0, damageToDistribute)
                                     .SetSpellValueMod(SpellValueMod.BasePoint1, damageToDistribute));
            }

            if (player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING, out dmgDelayRemaining) &&
                SpellManager.Instance.TryGetSpellInfo(WarlockSpells.FEL_ARMOR_DMG_DELAY, out var si) &&
                si.TryGetEffect(0, out var spellEffectInfo))
            {
                var duration = dmgDelayRemaining.Duration;
                var numTicks = duration / spellEffectInfo.ApplyAuraPeriod;

                if (player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY, out var pDamageAura))
                {
                    pDamageAura.GetEffect(0).SetAmount(damageToDistribute / numTicks);
                    pDamageAura.SetMaxDuration(duration);
                    pDamageAura.SetDuration(duration);
                }
                else
                {
                    player.SpellFactory.CastSpell(player, WarlockSpells.FEL_ARMOR_DMG_DELAY, damageToDistribute / numTicks, true);
                }
            }
        }

        return amount;
    }
}