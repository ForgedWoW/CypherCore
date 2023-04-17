// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Rogue;

[Script] // 212283 - Symbols of Death
internal class SpellRogSymbolsOfDeath : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEffectHitTarget, 0, SpellEffectName.ApplyAura, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEffectHitTarget(int effIndex)
    {
        if (Caster.HasAura(RogueSpells.SymbolsOfDeathRank2))
            Caster.SpellFactory.CastSpell(Caster, RogueSpells.SymbolsOfDeathCritAura, true);
    }
}