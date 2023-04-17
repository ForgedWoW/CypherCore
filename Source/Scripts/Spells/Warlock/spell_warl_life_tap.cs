// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Life Tap - 1454
[SpellScript(1454)]
public class SpellWarlLifeTap : SpellScript, IHasSpellEffects, ISpellCheckCast
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public SpellCastResult CheckCast()
    {
        if (Caster.HealthPct > 15.0f || Caster.HasAura(LifeTap.LIFE_TAP_GLYPH))
            return SpellCastResult.SpellCastOk;

        return SpellCastResult.Fizzle;
    }

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleOnHitTarget, 0, SpellEffectName.Energize, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleOnHitTarget(int effIndex)
    {
        PreventHitDefaultEffect(effIndex);
        // if (!GetCaster()->HasAura(LIFE_TAP_GLYPH))
        //   GetCaster()->EnergizeBySpell(GetCaster(), LIFE_TAP, int32(GetCaster()->GetMaxHealth() * GetSpellInfo()->GetEffect(uint::0).BasePoints / 100), PowerType.Mana); TODO REWRITE
    }

    public struct LifeTap
    {
        public const uint LIFE_TAP = 1454;
        public const uint LIFE_TAP_GLYPH = 63320;
    }
}