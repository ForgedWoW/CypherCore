// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Forged.RealmServer.Maps;
using Forged.RealmServer.Scripting.BaseScripts;
using Forged.RealmServer.Scripting.Interfaces.IMap;

namespace Forged.RealmServer.Entities;

class SplitByFactionMapScript : WorldMapScript, IMapOnCreate<Map>
{
	public SplitByFactionMapScript(string name, uint mapId) : base(name, mapId) { }

	public void OnCreate(Map map)
	{
		Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceAlliance, map.InstanceId == TeamIds.Alliance ? 1 : 0, false, map);
		Global.WorldStateMgr.SetValue(WorldStates.TeamInInstanceHorde, map.InstanceId == TeamIds.Horde ? 1 : 0, false, map);
	}
}