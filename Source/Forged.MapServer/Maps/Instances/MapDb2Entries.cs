// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System;
using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.M;
using Framework.Constants;

namespace Forged.MapServer.Maps.Instances;

public struct MapDb2Entries
{
    public MapRecord Map;
    public MapDifficultyRecord MapDifficulty;

    public MapDb2Entries(uint mapId, Difficulty difficulty, CliDB cliDB, DB2Manager db2Manager)
    {
        Map = cliDB.MapStorage.LookupByKey(mapId);
        MapDifficulty = db2Manager.GetMapDifficultyData(mapId, difficulty);
    }

    public MapDb2Entries(MapRecord map, MapDifficultyRecord mapDifficulty)
    {
        Map = map;
        MapDifficulty = mapDifficulty;
    }

    public Tuple<uint, uint> GetKey()
    {
        return Tuple.Create(MapDifficulty.MapID, (uint)MapDifficulty.LockID);
    }

    public bool IsInstanceIdBound()
    {
        return !Map.IsFlexLocking() && !MapDifficulty.IsUsingEncounterLocks();
    }
}