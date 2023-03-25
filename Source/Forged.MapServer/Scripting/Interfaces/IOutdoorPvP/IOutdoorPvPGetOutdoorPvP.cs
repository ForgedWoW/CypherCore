// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Maps;
using Forged.MapServer.OutdoorPVP;

namespace Forged.MapServer.Scripting.Interfaces.IOutdoorPvP;

public interface IOutdoorPvPGetOutdoorPvP : IScriptObject
{
	OutdoorPvP GetOutdoorPvP(Map map);
}