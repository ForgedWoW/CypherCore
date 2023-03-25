// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.T;

namespace Forged.MapServer.Spells;

class NodeGroup
{
	public TraitNodeGroupRecord Data;
	public List<TraitCondRecord> Conditions = new();
	public List<TraitCostRecord> Costs = new();
	public List<Node> Nodes = new();
}