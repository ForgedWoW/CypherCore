// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

//17364
[SpellScript(17364)]
public class SpellShaStormstrike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var target = HitUnit;

        if (target == null)
            return;

        if (Caster.HasAura(ShamanSpells.CRASHING_STORM_DUMMY) && Caster.HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
            Caster.SpellFactory.CastSpell(target, ShamanSpells.CRASHING_LIGHTNING_DAMAGE, true);

        if (Caster && Caster.HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
            Caster.SpellFactory.CastSpell(null, ShamanSpells.CRASH_LIGHTNING_PROC, true);
    }
}