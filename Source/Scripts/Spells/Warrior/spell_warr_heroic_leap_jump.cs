﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warrior;

[Script] // Heroic Leap (triggered by Heroic Leap (6544)) - 178368
internal class spell_warr_heroic_leap_jump : SpellScript, IHasSpellEffects
{
    public List<ISpellEffect> SpellEffects { get; } = new();


    public override void Register()
    {
        SpellEffects.Add(new EffectHandler(AfterJump, 1, SpellEffectName.JumpDest, SpellScriptHookType.EffectHit));
    }

    private void AfterJump(int effIndex)
    {
        if (Caster.HasAura(WarriorSpells.GLYPH_OF_HEROIC_LEAP))
            Caster.CastSpell(Caster, WarriorSpells.GLYPH_OF_HEROIC_LEAP_BUFF, true);

        if (Caster.HasAura(WarriorSpells.IMPROVED_HEROIC_LEAP))
            Caster.SpellHistory.ResetCooldown(WarriorSpells.TAUNT, true);
    }
}