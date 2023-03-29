// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Shaman;

//17364
[SpellScript(17364)]
public class spell_sha_stormstrike : SpellScript, ISpellOnHit
{
    public void OnHit()
    {
        var target = HitUnit;

        if (target == null)
            return;

        if (Caster.HasAura(ShamanSpells.CRASHING_STORM_DUMMY) && Caster.HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
            Caster.CastSpell(target, ShamanSpells.CRASHING_LIGHTNING_DAMAGE, true);

        if (Caster && Caster.HasAura(ShamanSpells.CRASH_LIGTHNING_AURA))
            Caster.CastSpell(null, ShamanSpells.CRASH_LIGHTNING_PROC, true);
    }
}