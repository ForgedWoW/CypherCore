// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.MapServer.Spells.Auras;

public class AuraLoadEffectInfo
{
	public Dictionary<int, double> Amounts = new();
	public Dictionary<int, double> BaseAmounts = new();
}