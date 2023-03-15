// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using Game.Scripting;
using Game.Scripting.Interfaces.IAura;
using System.Collections.Generic;

namespace Scripts.Spells.Evoker;

[SpellScript(EvokerSpells.LIFE_GIVERS_FLAME_AURA)]
public class aura_evoker_life_givers_flame : AuraScript, IAuraScriptValues
{
    public Dictionary<string, object> ScriptValues { get; } = new();
}