// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using Game.Spells;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS_ORB_AURA_3)]
internal class aura_evoker_stasis_orb_3 : AuraScript, IAuraOnApply, IAuraScriptValues
{
    public Dictionary<string, object> ScriptValues { get; } = new();

    public void AuraApply()
    {
        Caster.AddAura(EvokerSpells.STASIS_OVERRIDE_AURA);
    }
}