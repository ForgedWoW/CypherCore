// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(774)]
public class SpellDruRejuvenation : SpellScript, ISpellBeforeHit, ISpellAfterHit
{
    private int _mRejuvenationAura = 0;

    public void AfterHit()
    {
        var caster = Caster;

        if (caster == null)
            return;

        var target = HitUnit;

        if (target == null)
            return;

        var rejuvenationAura = target.GetAura(DruidSpells.Rejuvenation, caster.GUID);

        if (rejuvenationAura != null && _mRejuvenationAura > 0)
            rejuvenationAura.SetDuration(_mRejuvenationAura);

        var newRejuvenationAuraEffect = target.GetAuraEffect(DruidSpells.Rejuvenation, 0);

        if (newRejuvenationAuraEffect != null)
            if (caster.HasAura(SoulOfTheForestSpells.SoulOfTheForestResto))
            {
                newRejuvenationAuraEffect.SetAmount(newRejuvenationAuraEffect.Amount * 2);
                caster.RemoveAura(SoulOfTheForestSpells.SoulOfTheForestResto);
            }

        if (caster.HasAura(207383))
            caster.SpellFactory.CastSpell(caster, Spells.Abundance, true);
    }


    public void BeforeHit(SpellMissInfo missInfo)
    {
        var caster = Caster;

        if (caster == null)
            return;

        var target = HitUnit;

        if (target == null)
            return;

        if (caster.HasAura(SoulOfTheForestSpells.SoulOfTheForestResto))
            //      NewRejuvenationAuraEffect->SetAmount(NewRejuvenationAuraEffect->GetAmount() * 2);
            HitHeal = HitHeal * 2;

        //      caster->RemoveAura(SOUL_OF_THE_FOREST_RESTO);
        ///Germination
        if (caster.HasAura(155675) && target.HasAura(DruidSpells.Rejuvenation, caster.GUID))
        {
            var rejuvenationAura = target.GetAura(DruidSpells.Rejuvenation, caster.GUID);

            if (rejuvenationAura == null)
                return;

            if (!target.HasAura(155777, caster.GUID))
            {
                caster.SpellFactory.CastSpell(target, 155777, true);
                _mRejuvenationAura = rejuvenationAura.Duration;
            }
            else
            {
                var germinationAura = target.GetAura(155777, caster.GUID);
                ;

                if (germinationAura != null && rejuvenationAura != null)
                {
                    var germinationDuration = germinationAura.Duration;
                    var rejuvenationDuration = rejuvenationAura.Duration;

                    if (germinationDuration > rejuvenationDuration)
                    {
                        caster.AddAura(DruidSpells.Rejuvenation, target);
                    }
                    else
                    {
                        caster.SpellFactory.CastSpell(target, 155777, true);
                        _mRejuvenationAura = rejuvenationDuration;
                    }
                }
            }
        }
    }

    public struct Spells
    {
        public static uint Cultivation = 200390;
        public static uint CultivationHot = 200389;
        public static uint Germination = 155675;
        public static uint GerminationHot = 155777;
        public static uint Abundance = 207383;
        public static uint AbundanceBuff = 207640;
    }
}