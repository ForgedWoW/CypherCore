// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;
using Framework.Constants;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.PRECOGNITION)]
public class AuraEvokerPrecognition : AuraScript, IAuraOnProc, IAuraCheckProc
{
    public bool CheckProc(ProcEventInfo info)
    {
        if (!info.HitMask.HasFlag(ProcFlagsHit.Interrupt))
            return false;

        for (var i = CurrentSpellTypes.Generic; i < CurrentSpellTypes.Max; i++)
        {
            var spell = Caster.GetCurrentSpell(i);

            if (spell != null && spell.State == SpellState.Casting)
                return false;
        }

        return true;
    }

    public void OnProc(ProcEventInfo info)
    {
        Caster.AddAura(EvokerSpells.PRECOGNITION_AURA);
    }
}