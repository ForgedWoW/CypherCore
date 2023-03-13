// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;
using System.Collections.Generic;

namespace Game.Scripting.Interfaces.IAura;

public interface IAuraScriptValues : IAuraScript
{
	Dictionary<string, object> ScriptValues { get; }
}