// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.RealmServer.DataStorage;
using Game.Entities;
using Game.Common.Entities.Players;

namespace Forged.RealmServer.Scripting.Interfaces.IAreaTrigger;

public interface IAreaTriggerOnExit : IScriptObject
{
	bool OnExit(Player player, AreaTriggerRecord trigger);
}