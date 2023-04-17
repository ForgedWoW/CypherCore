// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script] // 32065 - Fungal Decay
internal class SpellGenDecayOverTimeFungalDecaySpellScript : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var aur = GetHitAura();

        aur?.SetStackAmount((byte)SpellInfo.StackAmount);
    }
}