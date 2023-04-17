// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Druid;

[SpellScript(61391)]
public class SpellDruTyphoon : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleKnockBack, 0, SpellEffectName.KnockBack, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleKnockBack(int effIndex)
    {
        // Glyph of Typhoon
        if (Caster.HasAura(DruidSpells.GlyphOfTyphoon))
            PreventHitDefaultEffect(effIndex);
    }
}