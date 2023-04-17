// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Rogue;

[SpellScript(13877)]
public class SpellRogueBladeFlurrySpellScript : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        if (HitUnit == ExplTargetUnit)
            HitDamage = 0;
    }
}