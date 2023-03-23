// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Game.DataStorage;
using Game.Common.DataStorage.Structs.M;

namespace Game.Common.DataStorage;

class MountTypeXCapabilityRecordComparer : IComparer<MountTypeXCapabilityRecord>
{
	public int Compare(MountTypeXCapabilityRecord left, MountTypeXCapabilityRecord right)
	{
		if (left.MountTypeID == right.MountTypeID)
			return left.OrderIndex.CompareTo(right.OrderIndex);

		return left.Id.CompareTo(right.Id);
	}
}
