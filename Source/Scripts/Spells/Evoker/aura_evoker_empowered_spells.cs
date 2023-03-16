// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System.Linq;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.GREEN_DREAM_BREATH,
            EvokerSpells.GREEN_DREAM_BREATH_2,
            EvokerSpells.BLUE_ETERNITY_SURGE,
            EvokerSpells.BLUE_ETERNITY_SURGE_2,
            EvokerSpells.RED_FIRE_BREATH,
            EvokerSpells.RED_FIRE_BREATH,
            EvokerSpells.GREEN_SPIRITBLOOM,
            EvokerSpells.GREEN_SPIRITBLOOM_2)]
public class aura_evoker_empowered_spells : AuraScript, IAuraOnApply
{
    public void AuraApply()
    {
        Aura.SetDuration((double)(Aura.SpellInfo.EmpowerStages.Sum(a => a.Value.DurationMs) + 1000), true, true);
    }
}