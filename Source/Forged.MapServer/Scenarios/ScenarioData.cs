﻿// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.S;

namespace Game.Scenarios;

public class ScenarioData
{
	public ScenarioRecord Entry;
	public Dictionary<byte, ScenarioStepRecord> Steps = new();
}