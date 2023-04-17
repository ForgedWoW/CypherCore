// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Objects;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(195457)]
public class SpellRogGrapplingHookSpellScript : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var caster = Caster;
        var dest = ExplTargetDest;

        if (caster == null || dest == null)
            return;

        caster.SpellFactory.CastSpell(new Position(dest.X, dest.Y, dest.Z), RogueSpells.GRAPPLING_HOOK_TRIGGER, true);
    }
}