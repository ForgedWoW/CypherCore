// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 264178 - Demonbolt
[SpellScript(264178)]
public class SpellWarlockDemonbolt : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleHit, 0, SpellEffectName.SchoolDamage, SpellScriptHookType.EffectHit));
    }

    private void HandleHit(int effIndex)
    {
        if (Caster)
        {
            Caster.SpellFactory.CastSpell(Caster, WarlockSpells.DEMONBOLT_ENERGIZE, true);
            Caster.SpellFactory.CastSpell(Caster, WarlockSpells.DEMONBOLT_ENERGIZE, true);
        }
    }
}