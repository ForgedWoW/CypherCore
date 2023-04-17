// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.DeathKnight;

[Script] // 46584 - Raise Dead
internal class SpellDkRaiseDead : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleDummy, 0, SpellEffectName.Dummy, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleDummy(int effIndex)
    {
        var spellId = DeathKnightSpells.RAISE_DEAD_SUMMON;

        if (Caster.HasAura(DeathKnightSpells.SLUDGE_BELCHER))
            spellId = DeathKnightSpells.SLUDGE_BELCHER_SUMMON;

        Caster.SpellFactory.CastSpell((Unit)null, spellId, true);
    }
}