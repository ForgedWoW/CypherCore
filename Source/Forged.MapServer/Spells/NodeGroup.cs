// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.T;

namespace Forged.MapServer.Spells;

public class NodeGroup
{
    public List<TraitCondRecord> Conditions { get; set; } = new();
    public List<TraitCostRecord> Costs { get; set; } = new();
    public TraitNodeGroupRecord Data { get; set; }
    public List<TraitNode> Nodes { get; set; } = new();
}