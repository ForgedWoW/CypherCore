// Copyright (c) Forged WoW LLC <https://github.com/ForgedWoW/ForgedCore>
// Licensed under GPL-3.0 license. See <https://github.com/ForgedWoW/ForgedCore/blob/master/LICENSE> for full information.

using Framework.Constants;
using Game.Common.Chat.Channels;
using Game.Common.Entities.Players;

namespace Game.Common.Scripting.Interfaces.IPlayer;

public interface IPlayerOnChatChannel : IScriptObject
{
	void OnChat(Player player, ChatMsg type, Language lang, string msg, Channel channel);
}
