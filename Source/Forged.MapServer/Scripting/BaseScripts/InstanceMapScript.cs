// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Forged.MapServer.Scripting.Interfaces;
using Serilog;

namespace Forged.MapServer.Scripting.BaseScripts;

public class InstanceMapScript : MapScript<InstanceMap>, IScriptAutoAdd
{
    public InstanceMapScript(string name, uint mapId) : base(name, mapId)
    {
        if (GetEntry() != null &&
            !GetEntry().IsDungeon())
            Log.Logger.Error("InstanceMapScript for map {0} is invalid.", mapId);
    }

    public override bool IsDatabaseBound()
    {
        return true;
    }
}