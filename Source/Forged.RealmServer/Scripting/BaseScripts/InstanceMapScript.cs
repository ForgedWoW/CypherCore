// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Maps;

namespace Forged.RealmServer.Scripting.BaseScripts;

public class InstanceMapScript : MapScript<InstanceMap>
{
	public InstanceMapScript(string name, uint mapId) : base(name, mapId)
	{
		if (GetEntry() != null &&
			!GetEntry().IsDungeon())
			Log.Logger.Error("InstanceMapScript for map {0} is invalid.", mapId);

		_scriptManager.AddScript(this);
	}

	public override bool IsDatabaseBound()
	{
		return true;
	}
}