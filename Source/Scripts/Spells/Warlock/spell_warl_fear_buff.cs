// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

//204730 - Fear (effect)
[SpellScript(204730)]
public class SpellWarlFearBuff : SpellScript, ISpellAfterHit
{
    public void AfterHit()
    {
        var aura = GetHitAura();

        if (aura != null)
        {
            aura.SetMaxDuration(20000);
            aura.SetDuration(20000);
            aura.RefreshDuration();
        }
    }
}