// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting;
using Forged.MapServer.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_DREAM_BREATH_CHARGED)]
internal class AuraEvokerDreamBreathCharged : AuraScript, IAuraOnApply
{
    public void AuraApply()
    {
        var aur = Aura;

        switch (aur.EmpoweredStage)
        {
            case 1:
                aur.SetDuration(12000, true, true);

                break;
            case 2:
                aur.SetDuration(8000, true, true);

                break;
            case 3:
                aur.SetDuration(4000, true, true);

                break;
            default:
                aur.SetDuration(16000, true, true);

                break;
        }
    }
}