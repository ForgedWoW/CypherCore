// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.Entities;
using Game.Common.Maps;
using Forged.RealmServer.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IMap;

public interface IMapOnPlayerLeave<T> : IScriptObject where T : Map
{
	void OnPlayerLeave(T map, Player player);
}