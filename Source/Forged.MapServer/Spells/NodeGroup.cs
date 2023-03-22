// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;

namespace Game;

class NodeGroup
{
	public TraitNodeGroupRecord Data;
	public List<TraitCondRecord> Conditions = new();
	public List<TraitCostRecord> Costs = new();
	public List<Node> Nodes = new();
}