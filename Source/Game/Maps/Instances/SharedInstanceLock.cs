// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Framework.Constants;

namespace Game.Maps;

class SharedInstanceLock : InstanceLock
{
	/// <summary>
	///  Instance id based locks have two states
	///  One shared by everyone, which is the real state used by instance
	///  and one for each player that shows in UI that might have less encounters completed
	/// </summary>
	readonly SharedInstanceLockData _sharedData;

	public SharedInstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId, SharedInstanceLockData sharedData) : base(mapId, difficultyId, expiryTime, instanceId)
	{
		_sharedData = sharedData;
	}

	public override InstanceLockData GetInstanceInitializationData()
	{
		return _sharedData;
	}

	public SharedInstanceLockData GetSharedData()
	{
		return _sharedData;
	}
}