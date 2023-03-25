// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Game.Common.Maps;
using Forged.RealmServer.PvP;

namespace Forged.RealmServer.Scripting.Interfaces.IOutdoorPvP;

public interface IOutdoorPvPGetOutdoorPvP : IScriptObject
{
	OutdoorPvP GetOutdoorPvP(Map map);
}