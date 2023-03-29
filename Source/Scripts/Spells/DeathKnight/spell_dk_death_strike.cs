// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;
using Game.Spells;

namespace Scripts.Spells.DeathKnight;

[Script] // 49998 - Death Strike
internal class spell_dk_death_strike : SpellScript, ISpellAfterCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public void AfterCast()
    {
        Caster.CastSpell(Caster, DeathKnightSpells.RecentlyUsedDeathStrike, true);
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 1, SpellEffectName.Dummy, SpellScriptHookType.Launch));
    }

    private void HandleDummy(int effIndex)
    {
        var caster = Caster;

        var enabler = caster.GetAuraEffect(DeathKnightSpells.DeathStrikeEnabler, 0, Caster.GUID);

        if (enabler != null)
        {
            // Heals you for 25% of all Damage taken in the last 5 sec,
            var heal = MathFunctions.CalculatePct(enabler.CalculateAmount(Caster), GetEffectInfo(1).CalcValue(Caster));
            // minimum 7.0% of maximum health.
            var pctOfMaxHealth = MathFunctions.CalculatePct(GetEffectInfo(2).CalcValue(Caster), caster.MaxHealth);
            heal = Math.Max(heal, pctOfMaxHealth);

            caster.CastSpell(caster, DeathKnightSpells.DeathStrikeHeal, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, heal));

            var aurEff = caster.GetAuraEffect(DeathKnightSpells.BloodShieldMastery, 0);

            if (aurEff != null)
                caster.CastSpell(caster, DeathKnightSpells.BloodShieldAbsorb, new CastSpellExtraArgs(TriggerCastFlags.FullMask).AddSpellMod(SpellValueMod.BasePoint0, MathFunctions.CalculatePct(heal, aurEff.Amount)));

            if (caster.HasAura(DeathKnightSpells.FROST))
                caster.CastSpell(HitUnit, DeathKnightSpells.DeathStrikeOffhand, true);
        }
    }
}