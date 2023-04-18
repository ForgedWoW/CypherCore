// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.T;
using Framework.Constants;

namespace Forged.MapServer.Spells;

public class Tree
{
    public TraitConfigType ConfigType { get; set; }
    public List<TraitCostRecord> Costs { get; set; } = new();
    public List<TraitCurrencyRecord> Currencies { get; set; } = new();
    public TraitTreeRecord Data { get; set; }
    public List<TraitNode> Nodes { get; set; } = new();
}