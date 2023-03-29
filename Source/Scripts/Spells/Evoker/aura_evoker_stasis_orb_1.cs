// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.STASIS_ORB_AURA_1)]
internal class aura_evoker_stasis_orb_1 : AuraScript, IAuraScriptValues
{
    public Dictionary<string, object> ScriptValues { get; } = new();
}