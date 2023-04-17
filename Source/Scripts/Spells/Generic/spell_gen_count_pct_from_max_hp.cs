// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;

namespace Scripts.Spells.Generic;

[Script("spell_gen_default_count_pct_from_max_hp", 0)]
[Script("spell_gen_50pct_count_pct_from_max_hp", 50)]
internal class SpellGenCountPctFromMaxHp : SpellScript, ISpellOnHit
{
    private double _damagePct;

    public SpellGenCountPctFromMaxHp(int damagePct)
    {
        _damagePct = damagePct;
    }

    public void OnHit()
    {
        if (_damagePct == 0)
            _damagePct = HitDamage;

        HitDamage = HitUnit.CountPctFromMaxHealth(_damagePct);
    }
}