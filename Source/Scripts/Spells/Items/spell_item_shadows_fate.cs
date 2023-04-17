// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Forged.MapServer.Spells;
using Framework.Constants;

namespace Scripts.Spells.Items;

[Script]
internal class SpellItemShadowsFate : AuraScript, IAuraOnProc
{
    public void OnProc(ProcEventInfo procInfo)
    {
        PreventDefaultAction();

        var caster = procInfo.Actor;
        var target = Caster;

        if (!caster ||
            !target)
            return;

        caster.SpellFactory.CastSpell(target, ItemSpellIds.SOUL_FEAST, new CastSpellExtraArgs(TriggerCastFlags.FullMask));
    }
}