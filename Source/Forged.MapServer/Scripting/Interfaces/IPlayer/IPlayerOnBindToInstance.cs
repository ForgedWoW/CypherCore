// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// Called when a player is bound to an instance
public interface IPlayerOnBindToInstance : IScriptObject
{
    void OnBindToInstance(Player player, Difficulty difficulty, uint mapId, bool permanent, byte extendState);
}