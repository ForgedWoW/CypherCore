// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warrior;

// 7384 - Overpower
[SpellScript(7384)]
public class SpellWarrOverpower : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();

    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(HandleEffect, 0, SpellEffectName.Any, SpellScriptHookType.EffectHitTarget));
    }

    private void HandleEffect(int effIndex)
    {
        if (!Caster)
            return;

        uint spellId = 0;

        if (Caster.HasAura(WarriorSpells.UNRELENTING_ASSAULT_RANK_1))
            spellId = WarriorSpells.UNRELENTING_ASSAULT_TRIGGER_1;
        else if (Caster.HasAura(WarriorSpells.UNRELENTING_ASSAULT_RANK_2))
            spellId = WarriorSpells.UNRELENTING_ASSAULT_TRIGGER_2;

        if (spellId == 0)
            return;

        var target = HitPlayer;

        if (target != null)
            if (target.IsNonMeleeSpellCast(false, false, true)) // UNIT_STATE_CASTING should not be used here, it's present during a tick for instant casts
                target.SpellFactory.CastSpell(target, spellId, true);
    }
}