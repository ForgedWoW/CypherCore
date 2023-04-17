// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Units;
using Forged.MapServer.Scripting;
using Framework.Constants;

namespace Scripts.Spells.Mage;

[SpellScript(157642)]
public class SpellMageEnhancedPyrotechnics : AuraScript
{
    private bool HandleProc(ProcEventInfo eventInfo)
    {
        var caster = Caster;

        if (eventInfo.SpellInfo.Id == MageSpells.FIREBALL)
        {
            if ((eventInfo.HitMask & ProcFlagsHit.Critical) != 0)
            {
                if (caster.HasAura(MageSpells.ENHANCED_PYROTECHNICS_AURA))
                    caster.RemoveAura(MageSpells.ENHANCED_PYROTECHNICS_AURA);

                return false;
            }

            return true;
        }

        return false;
    }
}