// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Paladin;

// Flash of Light - 19750
[SpellScript(19750)]
public class SpellPalFlashOfLight : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        Caster.RemoveAura(PaladinSpells.INFUSION_OF_LIGHT_AURA);
    }
}