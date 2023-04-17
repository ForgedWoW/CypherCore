// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script]
internal class SpellGenTriggerExcludeCasterAuraSpell : SpellScript, ISpellAfterCast
{
    public void AfterCast()
    {
        // Blizz seems to just apply aura without bothering to cast
        Caster.AddAura(SpellInfo.ExcludeCasterAuraSpell, Caster);
    }
}