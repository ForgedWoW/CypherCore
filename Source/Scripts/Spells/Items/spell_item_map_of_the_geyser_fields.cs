// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemMapOfTheGeyserFields : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        var caster = Caster;

        if (caster.FindNearestCreature(CreatureIds.SOUTH_SINKHOLE, 30.0f, true) ||
            caster.FindNearestCreature(CreatureIds.NORTHEAST_SINKHOLE, 30.0f, true) ||
            caster.FindNearestCreature(CreatureIds.NORTHWEST_SINKHOLE, 30.0f, true))
            return SpellCastResult.SpellCastOk;

        SetCustomCastResultMessage(SpellCustomErrors.MustBeCloseToSinkhole);

        return SpellCastResult.CustomError;
    }
}