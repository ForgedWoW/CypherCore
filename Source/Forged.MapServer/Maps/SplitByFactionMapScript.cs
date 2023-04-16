// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Scripting.BaseScripts;
using Forged.MapServer.Scripting.Interfaces.IMap;
using Framework.Constants;

namespace Forged.MapServer.Maps;

internal class SplitByFactionMapScript : WorldMapScript, IMapOnCreate<Map>
{
    public SplitByFactionMapScript(string name, uint mapId) : base(name, mapId) { }

    public void OnCreate(Map map)
    {
        map.WorldStateManager.SetValue(WorldStates.TeamInInstanceAlliance, map.InstanceId == TeamIds.Alliance ? 1 : 0, false, map);
        map.WorldStateManager.SetValue(WorldStates.TeamInInstanceHorde, map.InstanceId == TeamIds.Horde ? 1 : 0, false, map);
    }
}