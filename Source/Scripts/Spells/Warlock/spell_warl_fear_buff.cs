// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Warlock;

//204730 - Fear (effect)
[SpellScript(204730)]
public class spell_warl_fear_buff : SpellScript, ISpellAfterHit
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