// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player's level changes (after the level is applied);
public interface IPlayerOnLevelChanged : IScriptObject, IClassRescriction
{
	void OnLevelChanged(Player player, uint oldLevel);
}