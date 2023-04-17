// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(194153)]
public class SpellDruidLunarStrike : SpellScript, IHasSpellEffects
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

        if (Caster.HasAura(Spells.NaturesBalance))
        {
            var moonfireDot = currentTarget.GetAura(MoonfireSpells.MOONFIRE_DAMAGE, Caster.GUID);

            if (moonfireDot != null)
            {
                var duration = moonfireDot.Duration;
                var newDuration = duration + 6 * Time.IN_MILLISECONDS;

                if (newDuration > moonfireDot.MaxDuration)
                    moonfireDot.SetMaxDuration(newDuration);

                moonfireDot.SetDuration(newDuration);
            }
        }

        if (Caster && RandomHelper.randChance(20) && Caster.HasAura(DruidSpells.Eclipse))
            Caster.SpellFactory.CastSpell(null, DruidSpells.SolarEmpowerement, true);
    }

    private void HandleHit(int effIndex)
    {
        var warriorOfElune = Caster.GetAura(Spells.WarriorOfElune);

        if (warriorOfElune != null)
        {
            var amount = warriorOfElune.GetEffect(0).Amount;
            warriorOfElune.GetEffect(0).SetAmount(amount - 1);

            if (amount == -102)
                Caster.RemoveAura(Spells.WarriorOfElune);
        }
    }

    private struct Spells
    {
        public static readonly uint LunarStrike = 194153;
        public static readonly uint WarriorOfElune = 202425;
        public static readonly uint NaturesBalance = 202430;
    }
}