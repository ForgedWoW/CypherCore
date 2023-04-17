// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(86213)] // 86213 - Soul Swap Exhale
internal class SpellWarlSoulSwapExhale : SpellScript, ISpellCheckCast, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public SpellCastResult CheckCast()
    {
        var currentTarget = ExplTargetUnit;
        Unit swapTarget = null;
        var swapOverride = Caster.GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

        if (swapOverride != null)
        {
            var swapScript = swapOverride.GetScript<SpellWarlSoulSwapOverride>();

            if (swapScript != null)
                swapTarget = swapScript.GetOriginalSwapSource();
        }

        // Soul Swap Exhale can't be cast on the same Target than Soul Swap
        if (swapTarget &&
            currentTarget &&
            swapTarget == currentTarget)
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(OnEffectHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void OnEffectHit(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, WarlockSpells.SOUL_SWAP_MOD_COST, true);
        var hasGlyph = Caster.HasAura(WarlockSpells.GLYPH_OF_SOUL_SWAP);

        List<uint> dotList = new();
        Unit swapSource = null;
        var swapOverride = Caster.GetAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

        if (swapOverride != null)
        {
            var swapScript = swapOverride.GetScript<SpellWarlSoulSwapOverride>();

            if (swapScript == null)
                return;

            dotList = swapScript.GetDotList();
            swapSource = swapScript.GetOriginalSwapSource();
        }

        if (dotList.Empty())
            return;

        foreach (var itr in dotList)
        {
            Caster.AddAura(itr, HitUnit);

            if (!hasGlyph && swapSource)
                swapSource.RemoveAura(itr);
        }

        // Remove Soul Swap Exhale buff
        Caster.RemoveAura(WarlockSpells.SOUL_SWAP_OVERRIDE);

        if (hasGlyph) // Add a cooldown on Soul Swap if caster has the glyph
            Caster.SpellFactory.CastSpell(Caster, WarlockSpells.SOUL_SWAP_CD_MARKER, false);
    }
}