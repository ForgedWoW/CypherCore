// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Druid;

[SpellScript(194153)]
public class spell_druid_lunar_strike : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHitTarget, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
        SpellEffects.Add(new EffectHandler(HandleHit, 1, SpellEffectName.Energize, SpellScriptHookType.EffectHit));
    }

    private void HandleHitTarget(int effIndex)
    {
        var explTarget = ExplTargetUnit;
        var currentTarget = HitUnit;

        if (explTarget == null || currentTarget == null)
            return;

        if (currentTarget != explTarget)
            HitDamage = HitDamage * SpellInfo.GetEffect(2).BasePoints / 100;

        if (Caster.HasAura(Spells.NATURES_BALANCE))
        {
            var moonfireDOT = currentTarget.GetAura(MoonfireSpells.MOONFIRE_DAMAGE, Caster.GUID);

            if (moonfireDOT != null)
            {
                var duration = moonfireDOT.Duration;
                var newDuration = duration + 6 * Time.InMilliseconds;

                if (newDuration > moonfireDOT.MaxDuration)
                    moonfireDOT.SetMaxDuration(newDuration);

                moonfireDOT.SetDuration(newDuration);
            }
        }

        if (Caster && RandomHelper.randChance(20) && Caster.HasAura(DruidSpells.ECLIPSE))
            Caster.CastSpell(null, DruidSpells.SOLAR_EMPOWEREMENT, true);
    }

    private void HandleHit(int effIndex)
    {
        var WarriorOfElune = Caster.GetAura(Spells.WARRIOR_OF_ELUNE);

        if (WarriorOfElune != null)
        {
            var amount = WarriorOfElune.GetEffect(0).Amount;
            WarriorOfElune.GetEffect(0).SetAmount(amount - 1);

            if (amount == -102)
                Caster.RemoveAura(Spells.WARRIOR_OF_ELUNE);
        }
    }

    private struct Spells
    {
        public static readonly uint LUNAR_STRIKE = 194153;
        public static readonly uint WARRIOR_OF_ELUNE = 202425;
        public static readonly uint NATURES_BALANCE = 202430;
    }
}