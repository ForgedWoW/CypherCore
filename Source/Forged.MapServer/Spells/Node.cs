// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.T;
using Framework.Constants;

namespace Forged.MapServer.Spells;

internal class Node
{
    public List<TraitCondRecord> Conditions = new();
    public List<TraitCostRecord> Costs = new();
    public TraitNodeRecord Data;
    public List<NodeEntry> Entries = new();
    public List<NodeGroup> Groups = new();
    public List<Tuple<Node, TraitEdgeType>> ParentNodes = new(); // TraitEdge::LeftTraitNodeID
}