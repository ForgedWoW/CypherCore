// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Forged.MapServer.Maps;

namespace Forged.MapServer.Scripting.Interfaces.IMap;

public interface IMapOnPlayerLeave<T> : IScriptObject where T : Map
{
    void OnPlayerLeave(T map, Player player);
}