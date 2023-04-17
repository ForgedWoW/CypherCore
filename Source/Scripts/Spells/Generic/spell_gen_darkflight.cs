// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenDarkflight : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        Caster.SpellFactory.CastSpell(Caster, GenericSpellIds.ALTERED_FORM, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}