// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// Burning Rush - 111400
[SpellScript(111400)]
public class SpellWarlBurningRush : SpellScript, ISpellCheckCast, ISpellBeforeCast, ISpellAfterHit
{
    private bool _isRemove = false;

    public void AfterHit()
    {
        if (_isRemove)
            Caster.RemoveAura(WarlockSpells.BURNING_RUSH);
    }

    public void BeforeCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        if (caster.HasAura(WarlockSpells.BURNING_RUSH))
            _isRemove = true;
    }

    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster == null)
            return SpellCastResult.CantDoThatRightNow;

        if (caster.HealthBelowPct(5))
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }
}