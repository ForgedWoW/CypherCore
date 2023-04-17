// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Monk;

[SpellScript(119996)]
public class SpellMonkTranscendenceTransfer : SpellScript, ISpellOnCast, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster == null)
            return SpellCastResult.Error;

        Unit spirit = SpellMonkTranscendence.GetSpirit(caster);

        if (spirit == null)
        {
            SetCustomCastResultMessage(SpellCustomErrors.YouHaveNoSpiritActive);

            return SpellCastResult.CustomError;
        }

        if (!spirit.IsWithinDist(caster, SpellInfo.GetMaxRange(true, caster, Spell)))
            return SpellCastResult.OutOfRange;

        return SpellCastResult.SpellCastOk;
    }

    public void OnCast()
    {
        var caster = Caster;

        if (caster == null)
            return;

        Unit spirit = SpellMonkTranscendence.GetSpirit(caster);

        if (spirit == null)
            return;

        caster.NearTeleportTo(spirit.Location, true);
        spirit.NearTeleportTo(caster.Location, true);
    }
}