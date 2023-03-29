// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player's free talent points change (right before the change is applied);
public interface IPlayerOnFreeTalentPointsChanged : IScriptObject
{
    void OnFreeTalentPointsChanged(Player player, uint points);
}