// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using System.Collections.Generic;
using Forged.MapServer.DataStorage;
using Forged.MapServer.DataStorage.Structs.M;
using Forged.MapServer.Maps;

namespace Forged.MapServer.Scripting.BaseScripts;

public class MapScript<T> : ScriptObject where T : Map
{
	private readonly MapRecord _mapEntry;

	public MapScript(string name, uint mapId) : base(name)
	{
		_mapEntry = CliDB.MapStorage.LookupByKey(mapId);

		if (_mapEntry == null)
			Log.Logger.Error("Invalid MapScript for {0}; no such map ID.", mapId);
	}

	// Gets the MapEntry structure associated with this script. Can return NULL.
	public MapRecord GetEntry()
	{
		return _mapEntry;
	}
}