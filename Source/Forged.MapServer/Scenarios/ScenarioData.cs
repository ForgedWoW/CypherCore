// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.S;

namespace Forged.MapServer.Scenarios;

public class ScenarioData
{
    public ScenarioRecord Entry;
    public Dictionary<byte, ScenarioStepRecord> Steps = new();
}