// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;
using Framework.Models;

namespace Scripts.Spells.Rogue;

[SpellScript(31230)]
public class SpellRogCheatDeathAuraScript : AuraScript, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();


    public override bool Load()
    {
        return OwnerAsUnit.TypeId == TypeId.Player;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectCalcAmountHandler(CalculateAmount, 1, AuraType.SchoolAbsorb));
        AuraEffects.Add(new AuraEffectAbsorbHandler(Absorb, 1));
    }

    private void CalculateAmount(AuraEffect unnamedParameter, BoxedValue<double> amount, BoxedValue<bool> canBeRecalculated)
    {
        // Set absorbtion amount to unlimited
        amount.Value = -1;
    }

    private double Absorb(AuraEffect unnamedParameter, DamageInfo dmgInfo, double absorbAmount)
    {
        var target = Target.AsPlayer;

        if (target.HasAura(CheatDeath.CheatDeathDmgReduc))
        {
            return MathFunctions.CalculatePct(dmgInfo.Damage, 85);
        }
        else
        {
            if (dmgInfo.Damage < target.Health || target.HasAura(RogueSpells.CHEAT_DEATH_COOLDOWN))
                return absorbAmount;

            var health7 = target.CountPctFromMaxHealth(7);
            target.SetHealth(1);
            var healInfo = new HealInfo(target, target, (uint)health7, SpellInfo, SpellInfo.GetSchoolMask());
            target.HealBySpell(healInfo);
            target.SpellFactory.CastSpell(target, CheatDeath.CheatDeathAnim, true);
            target.SpellFactory.CastSpell(target, CheatDeath.CheatDeathDmgReduc, true);
            target.SpellFactory.CastSpell(target, RogueSpells.CHEAT_DEATH_COOLDOWN, true);
            absorbAmount = dmgInfo.Damage;
        }

        return absorbAmount;
    }
}