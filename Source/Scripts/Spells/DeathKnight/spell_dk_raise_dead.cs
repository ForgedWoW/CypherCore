// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.DeathKnight;

[Script] // 46584 - Raise Dead
internal class spell_dk_raise_dead : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var spellId = DeathKnightSpells.RaiseDeadSummon;

        if (Caster.HasAura(DeathKnightSpells.SludgeBelcher))
            spellId = DeathKnightSpells.SludgeBelcherSummon;

        Caster.CastSpell((Unit)null, spellId, true);
    }
}