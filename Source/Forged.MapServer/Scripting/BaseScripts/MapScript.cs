// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage.ClientReader;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Maps;
using Serilog;

namespace Forged.MapServer.Scripting.BaseScripts;

public class MapScript<T> : ScriptObject where T : Map
{
    private readonly uint _mapId;
    private MapRecord _mapEntry;
    private DB6Storage<MapRecord> _mapRecords;

    public MapScript(string name, uint mapId) : base(name)
    {
        _mapId = mapId;
    }

    // Gets the MapEntry structure associated with this script. Can return NULL.
    public MapRecord GetEntry()
    {
        if (_mapEntry != null || _mapRecords != null)
            return _mapEntry;

        _mapRecords = ClassFactory.Resolve<DB6Storage<MapRecord>>();
        _mapEntry = _mapRecords.LookupByKey(_mapId);

        if (_mapEntry == null)
            Log.Logger.Error("Invalid MapScript for {0}; no such map ID.", _mapId);

        return _mapEntry;
    }
}