// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Warlock;

// 212619 - Call Felhunter
[SpellScript(212619)]
public class SpellWarlockCallFelhunter : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster == null || !caster.AsPlayer)
            return SpellCastResult.BadTargets;

        if (caster.AsPlayer.CurrentPet && caster.AsPlayer.CurrentPet.Entry == 417)
            return SpellCastResult.CantDoThatRightNow;

        return SpellCastResult.SpellCastOk;
    }
}