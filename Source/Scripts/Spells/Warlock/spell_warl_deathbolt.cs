// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;
using Framework.Dynamic;

namespace Scripts.Spells.Warlock;

// 264106 - Deathbolt
[SpellScript(264106)]
public class SpellWarlDeathbolt : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        HitDamage = CalculateDamage();
    }

    private double CalculateDamage()
    {
        double damage = 0;
        var auras = HitUnit.GetAppliedAurasQuery();

        foreach (var aura in auras.HasSpellFamily(SpellFamilyNames.Warlock).GetResults())
        {
            var spell = aura.Base.SpellInfo;

            if (spell.SpellFamilyName == SpellFamilyNames.Warlock && (spell.SpellFamilyFlags & new FlagArray128(502, 8110, 300000, 0))) // out of Mastery : Potent Afflictions
            {
                var effects = aura.Base.AuraEffects;

                foreach (var iter in effects)
                    if (iter.Value.AuraType == AuraType.PeriodicDamage)
                    {
                        double valToUse = 0f;

                        if (spell.Id == WarlockSpells.CORRUPTION_DOT)
                            valToUse = iter.Value.GetRemainingAmount(SpellInfo.GetEffect(2).BasePoints * 1000);

                        damage += valToUse * SpellInfo.GetEffect(1).BasePoints / 100;
                    }
            }
        }

        return damage;
    }
}