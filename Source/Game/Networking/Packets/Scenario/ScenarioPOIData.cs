// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Scenarios;

namespace Game.Networking.Packets;

struct ScenarioPOIData
{
	public int CriteriaTreeID;
	public List<ScenarioPOI> ScenarioPOIs;
}