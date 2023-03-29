﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(195457)]
public class spell_rog_grappling_hook_SpellScript : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var dest = ExplTargetDest;

        if (caster == null || dest == null)
            return;

        caster.CastSpell(new Position(dest.X, dest.Y, dest.Z), RogueSpells.GRAPPLING_HOOK_TRIGGER, true);
    }
}