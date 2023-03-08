// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.Spells;

namespace Game.Entities;

class VisibleAuraSlotCompare : IComparer<AuraApplication>
{
	public int Compare(AuraApplication x, AuraApplication y)
	{
		return x.GetSlot().CompareTo(y.GetSlot());
	}
}