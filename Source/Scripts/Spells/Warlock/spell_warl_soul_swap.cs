// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

[SpellScript(86121)] // 86121 - Soul Swap
internal class SpellWarlSoulSwap : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleHit(int effIndex)
    {
        Caster.SpellFactory.CastSpell(Caster, WarlockSpells.SOUL_SWAP_OVERRIDE, true);
        HitUnit.SpellFactory.CastSpell(Caster, WarlockSpells.SOUL_SWAP_OVERRIDE, true);
    }
}