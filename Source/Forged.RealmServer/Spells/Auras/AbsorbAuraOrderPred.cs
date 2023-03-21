// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.Spells;

class AbsorbAuraOrderPred : Comparer<AuraEffect>
{
	public override int Compare(AuraEffect aurEffA, AuraEffect aurEffB)
	{
		var spellProtoA = aurEffA.SpellInfo;
		var spellProtoB = aurEffB.SpellInfo;

		// Fel Blossom
		if (spellProtoA.Id == 28527)
			return 1;

		if (spellProtoB.Id == 28527)
			return 0;

		// Ice Barrier
		if (spellProtoA.Category == 471)
			return 1;

		if (spellProtoB.Category == 471)
			return 0;

		// Sacrifice
		if (spellProtoA.Id == 7812)
			return 1;

		if (spellProtoB.Id == 7812)
			return 0;

		// Cauterize (must be last)
		if (spellProtoA.Id == 86949)
			return 0;

		if (spellProtoB.Id == 86949)
			return 1;

		// Spirit of Redemption (must be last)
		if (spellProtoA.Id == 20711)
			return 0;

		if (spellProtoB.Id == 20711)
			return 1;

		return 0;
	}
}