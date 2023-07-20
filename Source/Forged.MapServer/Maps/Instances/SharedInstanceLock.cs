// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using Forged.MapServer.DataStorage;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

internal class SharedInstanceLock : InstanceLock
{
    /// <summary>
    ///     Instance id based locks have two states
    ///     One shared by everyone, which is the real state used by instance
    ///     and one for each player that shows in UI that might have less encounters completed
    /// </summary>
    private readonly SharedInstanceLockData _sharedData;

    public SharedInstanceLock(uint mapId, Difficulty difficultyId, DateTime expiryTime, uint instanceId, SharedInstanceLockData sharedData, InstanceLockManager instanceLockManager, CliDB cliDB, DB2Manager db2Manager) : base(mapId, difficultyId, expiryTime, instanceId, instanceLockManager, cliDB, db2Manager)
    {
        _sharedData = sharedData;
    }

    public override InstanceLockData InstanceInitializationData => _sharedData;

    public SharedInstanceLockData GetSharedData()
    {
        return _sharedData;
    }
}