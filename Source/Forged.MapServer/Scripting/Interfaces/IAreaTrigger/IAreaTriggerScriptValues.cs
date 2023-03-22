﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Game.Scripting.Interfaces.IAreaTrigger;

public interface IAreaTriggerScriptValues : IAreaTriggerScript
{
	Dictionary<string, object> ScriptValues { get; }
}