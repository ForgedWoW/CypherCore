using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;

namespace Scripts.Spells.Warlock
{
    [SpellScript(WarlockSpells.SOUL_LEECH_ABSORB)]
    internal class aura_warl_soul_leech_absorb : AuraScript, IHasAuraEffects
    {
        public List<IAuraEffectHandler> AuraEffects { get; } = new List<IAuraEffectHandler>();

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
                    player.CastSpell(player, WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING, new CastSpellExtraArgs(true)
                                                                                            .SetSpellValueMod(SpellValueMod.BasePoint0, damageToDistribute)
                                                                                            .SetSpellValueMod(SpellValueMod.BasePoint1, damageToDistribute));

                if (player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY_REMAINING, out dmgDelayRemaining) &&
                    SpellManager.Instance.TryGetSpellInfo(WarlockSpells.FEL_ARMOR_DMG_DELAY, out var si) &&
                    si.TryGetEffect(0, out var spellEffectInfo))
                {
                    var duration = dmgDelayRemaining.GetDuration();
                    var numTicks = duration / spellEffectInfo.ApplyAuraPeriod;

                    if (player.TryGetAura(WarlockSpells.FEL_ARMOR_DMG_DELAY, out var pDamageAura))
                    {
                        pDamageAura.GetEffect(0).SetAmount(damageToDistribute / numTicks);
                        pDamageAura.SetMaxDuration(duration);
                        pDamageAura.SetDuration(duration);
                    }
                    else
                        player.CastSpell(player, WarlockSpells.FEL_ARMOR_DMG_DELAY, damageToDistribute / numTicks, true);
                }

            }

            return amount;
        }

        public override void Register()
        {
            AuraEffects.Add(new AuraEffectAbsorbHandler(OnAbsorb, 0));
        }
    }
}
