// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Entities;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.ISpell;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemGiftOfTheHarvester : SpellScript, ISpellCheckCast
{
    public SpellCastResult CheckCast()
    {
        List<TempSummon> ghouls = new();
        Caster.GetAllMinionsByEntry(ghouls, CreatureIds.GHOUL);

        if (ghouls.Count >= CreatureIds.MAX_GHOULS)
        {
            SetCustomCastResultMessage(SpellCustomErrors.TooManyGhouls);

            return SpellCastResult.CustomError;
        }

        return SpellCastResult.SpellCastOk;
    }
}