// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.PRECOGNITION)]
public class aura_evoker_precognition : AuraScript, IAuraOnProc, IAuraCheckProc
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