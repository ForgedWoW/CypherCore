// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Forged.MapServer.Scripting.Interfaces;
using Serilog;

namespace Forged.MapServer.Scripting.BaseScripts;

public class BattlegroundMapScript : MapScript<BattlegroundMap>, IScriptAutoAdd
{
    public BattlegroundMapScript(string name, uint mapId) : base(name, mapId)
    {
        if (GetEntry() != null &&
            GetEntry().IsBattleground())
            Log.Logger.Error("BattlegroundMapScript for map {0} is invalid.", mapId);
    }
}