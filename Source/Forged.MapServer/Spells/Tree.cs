// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Framework.Constants;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.T;

namespace Game;

class Tree
{
	public TraitTreeRecord Data;
	public List<Node> Nodes = new();
	public List<TraitCostRecord> Costs = new();
	public List<TraitCurrencyRecord> Currencies = new();
	public TraitConfigType ConfigType;
}