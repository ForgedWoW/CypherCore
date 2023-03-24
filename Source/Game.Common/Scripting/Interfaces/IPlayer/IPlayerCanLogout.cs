// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Entities.Players;

namespace Game.Common.Scripting.Interfaces.IPlayer;

// The following methods are called when a player sends a chat message.
public interface IPlayerCanLogout : IScriptObject
{
	bool CanLogout(Player player);
}
