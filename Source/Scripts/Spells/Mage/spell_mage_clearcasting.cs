// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells.Auras;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(79684)]
public class SpellMageClearcasting : AuraScript, IAuraCheckProc, IHasAuraEffects
{
    public List<IAuraEffectHandler> AuraEffects { get; } = new();

    public bool CheckProc(ProcEventInfo eventInfo)
    {
        var eff0 = SpellInfo.GetEffect(0).CalcValue();

        if (eff0 != 0)
        {
            double reqManaToSpent = 0;
            var manaUsed = 0;

            // For each ${$c*100/$s1} mana you spend, you have a 1% chance
            // Means: I cast a spell which costs 1000 Mana, for every 500 mana used I have 1% chance =  2% chance to proc
            foreach (var powerCost in SpellInfo.CalcPowerCost(Caster, SpellInfo.SchoolMask))
                if (powerCost.Power == PowerType.Mana)
                    reqManaToSpent = powerCost.Amount * 100 / eff0;

            // Something changed in DBC, Clearcasting should cost 1% of base mana 8.0.1
            if (reqManaToSpent == 0)
                return false;

            foreach (var powerCost in eventInfo.SpellInfo.CalcPowerCost(Caster, eventInfo.SpellInfo.SchoolMask))
                if (powerCost.Power == PowerType.Mana)
                    manaUsed = powerCost.Amount;

            var chance = Math.Floor(manaUsed / reqManaToSpent * (double)1);

            return RandomHelper.randChance(chance);
        }

        return false;
    }

    public override void Register()
    {
        AuraEffects.Add(new AuraEffectProcHandler(HandleProc, 0, AuraType.Dummy, AuraScriptHookType.EffectProc));
    }

    private void HandleProc(AuraEffect unnamedParameter, ProcEventInfo eventInfo)
    {
        var actor = eventInfo.Actor;
        actor.SpellFactory.CastSpell(actor, MageSpells.CLEARCASTING_BUFF, true);

        if (actor.HasAura(MageSpells.ARCANE_EMPOWERMENT))
            actor.SpellFactory.CastSpell(actor, MageSpells.CLEARCASTING_PVP_STACK_EFFECT, true);
        else
            actor.SpellFactory.CastSpell(actor, MageSpells.CLEARCASTING_EFFECT, true);
    }
}