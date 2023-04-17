// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

[SpellScript(114165)] // 114165 - Holy Prism
internal class SpellPalHolyPrism : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        if (Caster.IsFriendlyTo(HitUnit))
            Caster.SpellFactory.CastSpell(HitUnit, PaladinSpells.HOLY_PRISM_TARGET_ALLY, true);
        else
            Caster.SpellFactory.CastSpell(HitUnit, PaladinSpells.HOLY_PRISM_TARGET_ENEMY, true);

        Caster.SpellFactory.CastSpell(HitUnit, PaladinSpells.HOLY_PRISM_TARGET_BEAM_VISUAL, true);
    }
}