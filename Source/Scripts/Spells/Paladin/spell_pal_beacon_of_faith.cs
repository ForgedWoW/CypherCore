// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Paladin;

// Beacon of Faith - 156910
[SpellScript(156910)]
public class SpellPalBeaconOfFaith : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var target = ExplTargetUnit;

        if (target == null)
            return SpellCastResult.DontReport;

        if (target.HasAura(PaladinSpells.BEACON_OF_LIGHT))
            return SpellCastResult.BadTargets;

        return SpellCastResult.SpellCastOk;
    }
}