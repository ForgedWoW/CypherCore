// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.Scenarios;

namespace Forged.MapServer.Networking.Packets.Scenario;

internal struct ScenarioPOIData
{
    public int CriteriaTreeID;
    public List<ScenarioPOI> ScenarioPOIs;
}