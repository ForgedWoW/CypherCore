// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.T;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class TraitNode
{
    public List<TraitCondRecord> Conditions { get; set; } = new();
    public List<TraitCostRecord> Costs { get; set; } = new();
    public TraitNodeRecord Data { get; set; }
    public List<NodeEntry> Entries { get; set; } = new();
    public List<NodeGroup> Groups { get; set; } = new();
    public List<Tuple<TraitNode, TraitEdgeType>> ParentNodes { get; set; } = new(); // TraitEdge::LeftTraitNodeID
}