// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player's reputation changes (before it is actually changed);
public interface IPlayerOnReputationChange : IScriptObject
{
	void OnReputationChange(Player player, uint factionId, int standing, bool incremental);
}