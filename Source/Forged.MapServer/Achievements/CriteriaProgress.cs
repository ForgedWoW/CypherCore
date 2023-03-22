// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Entities;

namespace Game.Achievements;

public class CriteriaProgress
{
	public ulong Counter;
	public long Date;             // latest update time.
	public ObjectGuid PlayerGUID; // GUID of the player that completed this criteria (guild achievements)
	public bool Changed;
}