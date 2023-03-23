// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.T;

namespace Game;

class Node
{
	public TraitNodeRecord Data;
	public List<NodeEntry> Entries = new();
	public List<NodeGroup> Groups = new();
	public List<Tuple<Node, TraitEdgeType>> ParentNodes = new(); // TraitEdge::LeftTraitNodeID
	public List<TraitCondRecord> Conditions = new();
	public List<TraitCostRecord> Costs = new();
}