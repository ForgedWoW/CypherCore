// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player completes a movie
public interface IPlayerOnMovieComplete : IScriptObject
{
    void OnMovieComplete(Player player, uint movieId);
}