// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.Structs.F;

namespace Forged.MapServer.DataStorage;

class FriendshipRepReactionRecordComparer : IComparer<FriendshipRepReactionRecord>
{
	public int Compare(FriendshipRepReactionRecord left, FriendshipRepReactionRecord right)
	{
		return left.ReactionThreshold.CompareTo(right.ReactionThreshold);
	}
}