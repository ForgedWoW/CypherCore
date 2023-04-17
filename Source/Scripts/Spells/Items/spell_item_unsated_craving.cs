// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script] // 71169 - Shadow's Fate (Shadowmourne questline)
internal class SpellItemUnsatedCraving : AuraScript, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo procInfo)
    {
        var caster = procInfo.Actor;

        if (!caster ||
            caster.TypeId != TypeId.Player)
            return false;

        var target = procInfo.ActionTarget;

        if (!target ||
            target.TypeId != TypeId.Unit ||
            target.IsCritter ||
            (target.Entry != CreatureIds.SINDRAGOSA && target.IsSummon))
            return false;

        return true;
    }
}