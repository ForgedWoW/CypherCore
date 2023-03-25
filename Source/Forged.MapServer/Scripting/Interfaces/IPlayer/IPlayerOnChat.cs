// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Forged.MapServer.Entities.Players;
using Framework.Constants;

namespace Forged.MapServer.Scripting.Interfaces.IPlayer;

// The following methods are called when a player sends a chat message.
public interface IPlayerOnChat : IScriptObject
{
	void OnChat(Player player, ChatMsg type, Language lang, string msg);
}