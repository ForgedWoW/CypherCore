// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;

namespace Forged.RealmServer.DataStorage;

class ChrClassesXPowerTypesRecordComparer : IComparer<ChrClassesXPowerTypesRecord>
{
	public int Compare(ChrClassesXPowerTypesRecord left, ChrClassesXPowerTypesRecord right)
	{
		if (left.ClassID != right.ClassID)
			return left.ClassID.CompareTo(right.ClassID);

		return left.PowerType.CompareTo(right.PowerType);
	}
}