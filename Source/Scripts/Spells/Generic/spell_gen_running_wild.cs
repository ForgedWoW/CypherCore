// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenRunningWild : SpellScript
{
    public override bool Load()
    {
        // Definitely not a good thing, but currently the only way to do something at cast start
        // Should be replaced as soon as possible with a new hook: BeforeCastStart
        Caster.SpellFactory.CastSpell(Caster, GenericSpellIds.ALTERED_FORM, new CastSpellExtraArgs(TriggerCastFlags.FullMask));

        return false;
    }

    public override void Register() { }
}